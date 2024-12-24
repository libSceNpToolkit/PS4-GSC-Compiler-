﻿using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Parsing;
using T7CompilerLib.ScriptComponents;
using TreyarchCompiler.Games.BO3;
using TreyarchCompiler.Interface;
using TreyarchCompiler.Utilities;

// These allow us to hotswap the global definitions for these enums, which may be necessary as new games are added.
using ScriptContext = T7CompilerLib.ScriptContext;
using ScriptOpCode = T7CompilerLib.OpCodes.ScriptOpCode;
using ImportFlags = T7CompilerLib.ScriptComponents.T7Import.T7ImportFlags;
using ExportFlags = T7CompilerLib.ScriptExportFlags;
using T7ScriptObject = T7CompilerLib.T7ScriptObject;
using T7CompilerLib.OpCodes;
using T7CompilerLib;

namespace TreyarchCompiler.Games
{
    internal class GSCCompiler : BLOPSCompilerBase, ICompiler
    {
        private static bool EnableStatPtrProtect = true;

        /// <summary>
        /// This sucks. This is the best way since our terms are not static
        /// </summary>
        private const string CALL_PTR_TERMNAME = "baseCallPointer";
        private uint ScriptNamespace = 0xDEADBEEF;

        protected virtual dynamic NewScript => new T7ScriptObject(true);
        private dynamic Script;

        private readonly Dictionary<string, ScriptFunctionMetaData> FunctionMetadata;
        private readonly Dictionary<string, ParseTreeNode> Macros = new Dictionary<string, ParseTreeNode>();

        private readonly bool LittleEndian = false;

        private readonly Stack<QOperand> ScriptOperands = new Stack<QOperand>();

        private Dictionary<string, string> Func_StatProtectMap = new Dictionary<string, string>();
        private HashSet<string> CustomInjects = new HashSet<string>();

        public GSCCompiler(string code, string path, bool uset8masking)
        {
            FunctionMetadata = new Dictionary<string, ScriptFunctionMetaData>();
            _path = path;
            uset8masking = false;
            LittleEndian = true;

            Script = NewScript;
            Script.UseMasking = uset8masking;
            code = AppendProtectionSource(code);

            _tree = ParseCode(code);
        }

        private string AppendProtectionSource(string code)
        {
            return code;
        }

        private ParseTree ParseCode(string code)
        {
            return BO3Syntax.ParseCode(code);
        }

        /// <summary>
        /// Type cast for T7 to allow better type safety while designing dynamic code
        /// </summary>
        /// <returns></returns>
        private T7ScriptObject T7()
        {
            return Script as T7ScriptObject;
        }

        public CompiledCode Compile()
        {
            var ticks = DateTime.Now.Ticks;

            var data = new CompiledCode();
            if (T7().UseMasking) data.OpcodeMap = T7().Randomize();
            try
            {
                CompileTree();
            }
            catch (Exception ex)
            {
                data.Error = ex.Message;
                return data;
            }

            var assemble_ticks = DateTime.Now.Ticks;
            try
            {
                data.CompiledScript = Script.Serialize();
                data.HashMap = Script.GetHashMap();
            }
            catch (Exception ex) { data.Error = ex.ToString(); }
            var finalticks = DateTime.Now.Ticks;

            //Temporary debugging stats to keep track of compiler speed
            Console.WriteLine($"{ TimeSpan.FromTicks(finalticks - ticks).TotalMilliseconds } ms compile time (excluding irony)");
            Console.WriteLine($" -- { TimeSpan.FromTicks(assemble_ticks - ticks).TotalMilliseconds } ms to build the structure.");
            Console.WriteLine($" -- { TimeSpan.FromTicks(finalticks - assemble_ticks).TotalMilliseconds } ms to commit to binary.");
            //End of temp debugging stats

            //data.MaskData = new Dictionary<int, byte[]> { { (int)Masks.Opcodes, GetOpCodeArray().ToByteArray(Game) } };
            //data.WriteData = GetWriteData(Platform);

            return data;
        }
        public CompiledCode Compile(string address)
        {
            return null;
        }

        private void CompileTree()
        {
            if (_tree.HasErrors())
                throw new Exception($"Syntax error in input script! [line={_tree.ParserMessages[0].Location.Line}]");

            Script.Name.Value = "";//"scripts\\" + Guid.NewGuid().ToString().Replace("-", "_");

            SetNamespace();

            var functionTree = new Dictionary<string, ParseTreeNode>();

            if (_tree.Root.ChildNodes[0].ChildNodes.Count > 0)
                foreach (var directive in _tree.Root.ChildNodes[0].ChildNodes[0].ChildNodes.OrderBy(x => x.ChildNodes[0].Term.Name.ToLower() == "functions"))
                {
                    byte flags = (byte)ExportFlags.Private;
                    var FunctionFrame = directive;
                    switch (directive.ChildNodes[0].Term.Name.ToLower())
                    {
                        case "includes":
                            foreach (var node in directive.ChildNodes[0].ChildNodes)
                                Script.Includes.Add(NormalizeUsing(node.Token.ValueString));
                            break;

                        case "globals":
                            Macros[directive.ChildNodes[0].ChildNodes[0].FindTokenAndGetText().ToLower()] = directive.ChildNodes[0].ChildNodes[2];
                            break;

                        case "functionframe":

                            FunctionFrame = directive.ChildNodes[0];
                            if (FunctionFrame.ChildNodes[0].Term.Name == "autoexec")
                                flags |= (byte)ExportFlags.AutoExec;

                            goto functionsLabel;

                        case "functions":
                        functionsLabel:
                            var function = FunctionFrame.ChildNodes[FunctionFrame.ChildNodes.Count - 1];
                            var functionName = function.ChildNodes[function.ChildNodes.FindIndex(e => e.Term.Name == "identifier")].Token.ValueString.ToLower();
                            var Parameters = function.ChildNodes[function.ChildNodes.FindIndex(e => e.Term.Name == "parameters")].ChildNodes[0].ChildNodes;

                            if (FunctionMetadata.ContainsKey(functionName))
                                throw new ArgumentException($"Function '{functionName}' has been defined more than once.");

                            functionTree.Add(functionName, function);
                            FunctionMetadata[functionName] = new ScriptFunctionMetaData()
                            {
                                FunctionHash = Script.ScriptHash(functionName),
                                NamespaceHash = ScriptNamespace,
                                FunctionName = functionName,
                                NamespaceName = "ilcustom",
                                NumParams = (byte)Parameters.Count,
                                Flags = flags
                            };

                            break;
                    }
                }

            //Iterate over all function declarations
            foreach (var item in functionTree)
            {
                _currentDeclaration = item.Key;
                EmitFunction(item.Value, item.Key);
            }
        }

        private void EmitFunction(ParseTreeNode functionNode, string FunctionName)
        {
            var Parameters = functionNode.ChildNodes[functionNode.ChildNodes.FindIndex(e => e.Term.Name == "parameters")].ChildNodes[0].ChildNodes;

            dynamic CurrentFunction = CreateFunction(functionNode, FunctionName);
            CurrentFunction.Flags = FunctionMetadata[FunctionName].Flags;
            CurrentFunction.FriendlyName = FunctionName;

            foreach (var paramNode in Parameters)
                AddLocal(CurrentFunction, paramNode.FindTokenAndGetText().ToLower());

            IEnumerable<string> locals = CollectLocalVariables(CurrentFunction, functionNode.ChildNodes[functionNode.ChildNodes.FindIndex(e => e.Term.Name == "block")], false);

            foreach (var variable in locals)
                AddLocal(CurrentFunction, variable.ToLower());

            ScriptOperands.Clear();
            bool IsCustomInject = CustomInjects.Contains(FunctionName.ToLower());

            EmitOptionalParameters(CurrentFunction, Parameters, IsCustomInject);
            ScriptOperands.Clear();

            Push(CurrentFunction, functionNode.ChildNodes[functionNode.ChildNodes.FindIndex(e => e.Term.Name == "block")], 0);
            IterateStack(IsCustomInject);

            CurrentFunction.AddOp(DynOp(ScriptOpCode.End));
        }

        private void IterateStack(bool IsCustomInject)
        {
            while (ScriptOperands.Count > 0)
            {
                var CurrentOp = ScriptOperands.Pop();

                if (!CurrentOp.IsParseNode)
                    continue; //Stack misalignment

                var node = CurrentOp.ObjectNode;
                var CurrentFunction = CurrentOp.CurrentFunction;
                var Context = CurrentOp.Context;

                if (CurrentOp.GetOperands != null)
                {
                    if (!CurrentOp.GetOperands.MoveNext())
                        continue;

                    Push(CurrentOp);
                    Push(CurrentOp.GetOperands.Current);
                    continue;
                }

                if (IsCustomInject)
                    Context |= (byte)ScriptContext.IsCustomInject;

                switch (node.Term.Name)
                {
                    case "statement":
                    case "statementBlock":
                    case "declaration":
                    case "parenExpr":
                    case "block":
                        if (node.ChildNodes.Count > 0)
                            Push(CurrentOp.Replace(0));
                        break;

                    case "blockContent":
                        foreach (var child in node.ChildNodes[0].ChildNodes.AsEnumerable().Reverse())
                            Push(CurrentFunction, child, Context);
                        break;

                    case "jumpStatement":
                        int offset = 1;

                        if (node.ChildNodes.Count > 1)
                            offset = (int)node.ChildNodes[1].Token.Value;

                        offset = Math.Max(1, offset);
                        offset--;
                        if (node.ChildNodes[0].Term.Name == "continue")
                            CurrentFunction.PushLCF(true, offset);
                        else
                            CurrentFunction.PushLCF(false, offset);
                        break;

                    case "newArray":
                        CurrentFunction.AddOp(DynOp(ScriptOpCode.GetEmptyArray));
                        break;

                    case "array":
                        CurrentOp.SetOperands = EmitArray(CurrentOp);
                        Push(CurrentOp);
                        break;

                    case "shortHandArray":
                        throw new NotImplementedException("An array shorthand was passed in an invalid context to the node handler.");

                    case "ifStatement":
                        int count = node.ChildNodes.Count;

                        CurrentOp.SetOperands = EmitConditionalJump(CurrentFunction, node.ChildNodes[1], node.ChildNodes[2], count == 4 ? node.ChildNodes[3].ChildNodes[1] : null);
                        Push(CurrentOp);
                        break;

                    case "whileStatement":
                        CurrentOp.SetOperands = EmitWhile(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "forStatement":
                        CurrentOp.SetOperands = EmitForLoop(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "conditionalStatement":
                        CurrentOp.SetOperands = EmitConditionalJump(CurrentFunction, node.ChildNodes[0], node.ChildNodes[2], node.ChildNodes[4]);
                        Push(CurrentOp);
                        break;

                    case "switchStatement":
                        CurrentOp.SetOperands = EmitSwitchStatement(CurrentFunction, node);
                        Push(CurrentOp);
                        break;

                    case "simpleCall":
                        Push(CurrentFunction, node.ChildNodes[0], (uint)ScriptContext.DecTop);
                        break;

                    case "call":
                        CurrentOp.SetOperands = EmitCall(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "wait":
                        CurrentOp.SetOperands = EmitWait(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "return":
                        CurrentOp.SetOperands = EmitReturn(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "waitTillFrameEnd":
                        if (node.ChildNodes[0].Token.ValueString == "waittillframeend")
                            CurrentFunction.AddOp(DynOp(ScriptOpCode.WaitTillFrameEnd));
                        break;

                    case "setVariableField":
                        CurrentOp.SetOperands = EmitSVFOrShorthand(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "directAccess":
                        CurrentOp.SetOperands = EmitEvalFieldVariable(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "foreachSingle":
                    case "foreachDouble":
                        CurrentOp.SetOperands = EmitForeach(CurrentFunction, node);
                        Push(CurrentOp);
                        break;

                    case "booleanExpression":
                        CurrentOp.SetOperands = EmitBoolExpr(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "boolNot":
                        CurrentOp.SetOperands = EmitBoolNot(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "size":
                        CurrentOp.SetOperands = EmitSizeof(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "relationalExpression":
                        CurrentOp.SetOperands = EmitRelationalExpression(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "include_identifier":
                    case "identifier":
                        string LocalToLower = node.Token.ValueString.ToLower();
                        if (Macros.TryGetValue(LocalToLower, out ParseTreeNode MacroNode))
                            Push(CurrentFunction, MacroNode, Context);
                        else
                            AddEvalLocal(CurrentFunction, LocalToLower, HasContext(Context, ScriptContext.IsRef), HasContext(Context, ScriptContext.Waittill));
                        break;

                    case "stringLiteral":
                        AddGetString(CurrentFunction, node.Token.ValueString);
                        break;

                    case "hashedString":

                        string hashval = node.ChildNodes[1].Token.ValueString.ToLower().Replace("hash_", "");
                        try
                        {
                            CurrentFunction.AddGetHash(uint.Parse(hashval, System.Globalization.NumberStyles.HexNumber));
                        }
                        catch
                        {
                            throw new ArgumentException("Tried to hash string '" + node.ChildNodes[1].Token.ValueString.ToLower() + "', but it is not a hash value.");
                        }
                        break;

                    case "iString":
                        CurrentFunction.AddGetString(Script.Strings.AddString(node.ChildNodes[1].Token.ValueString), true);
                        break;

                    case "numberLiteral":
                        CurrentFunction.AddGetNumber(node.Token.Value);
                        break;

                    case "expression+":
                    case "expression":
                        CurrentOp.SetOperands = EmitExpression(CurrentFunction, node, Context);
                        Push(CurrentOp);
                        break;

                    case "getFunction":
                        EmitFunctionPtr(CurrentFunction, node, 0, HasContext(Context, ScriptContext.IsCustomInject));
                        break;

                    case "vector":
                        CurrentOp.SetOperands = EmitVector(CurrentFunction, node, Context);

                        Push(CurrentOp);
                        break;

                    default:
                        foreach (var child in node.ChildNodes.AsEnumerable().Reverse())
                            Push(CurrentFunction, child, Context);
                        break;
                }
            }
        }

        private IEnumerable<QOperand> EmitVector(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.Vector));
        }

        private void EmitFunctionPtr(dynamic CurrentFunction, ParseTreeNode node, byte Numparams, bool NoRefReplace)
        {
            ParseTreeNode FuncNameNode = node.ChildNodes[node.ChildNodes.Count - 1];
            ParseTreeNode NSNode = null;

            if (node.ChildNodes.Count > 1)
                NSNode = node.ChildNodes[0];

            uint t7_ns = ScriptNamespace;

            if (NSNode != null)
            {
                t7_ns = Script.ScriptHash(NSNode.ChildNodes[0].FindTokenAndGetText().ToLower());
            }

            byte Flags = (byte)ImportFlags.IsRef;

            if (t7_ns == ScriptNamespace)
                Flags |= (byte)ImportFlags.NeedsResolver;

            string fname = FuncNameNode.ChildNodes[0].FindTokenAndGetText().ToLower();
            uint FunctionID = Script.ScriptHash(fname);

            if (NoRefReplace || !T7().IsStatPtrProtected(FunctionID) || !EnableStatPtrProtect)
            {
                CurrentFunction.AddFunctionPtr(Script.Imports.AddImport(FunctionID, t7_ns, Numparams, Flags));
                return;
            }

            fname = Func_StatProtectMap[fname];
            t7_ns = ScriptNamespace;
            FunctionID = Script.ScriptHash(fname);
            Flags |= (byte)ImportFlags.NeedsResolver;

            CurrentFunction.AddFunctionPtr(Script.Imports.AddImport(FunctionID, t7_ns, Numparams, Flags));
        }

        private IEnumerable<QOperand> EmitExpression(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            CurrentFunction.AddMathToken(node.ChildNodes[1].Term.Name);
        }

        private void AddGetString(dynamic CurrentFunction, string Value)
        {
            (CurrentFunction as T7ScriptExport).AddGetString(T7().Strings.AddString(Value));
        }

        private IEnumerable<QOperand> EmitRelationalExpression(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            CurrentFunction.AddCompareOp(node.ChildNodes[1].ChildNodes[0].Term.Name);
        }

        private IEnumerable<QOperand> EmitSizeof(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.SizeOf));
        }

        private IEnumerable<QOperand> EmitBoolNot(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.BoolNot));
        }

        private IEnumerable<QOperand> EmitForeach(dynamic CurrentFunction, ParseTreeNode node)
        {
            if (!CurrentFunction.TryPopFEPair(out KeyValuePair<string, string> KeyPair))
                throw new InvalidOperationException("Tried to compile more foreach statements than were expected");

            int KeyIndex = node.ChildNodes.FindIndex(e => e.Term.Name == "key");

            string KeyName = KeyIndex != -1 ? node.ChildNodes[KeyIndex].FindTokenAndGetText().ToLower() : KeyPair.Value;
            string ValueName = node.ChildNodes[node.ChildNodes.FindIndex(e => e.Term.Name == "value")].FindTokenAndGetText().ToLower();
            string ArrayName = KeyPair.Key;

            yield return new QOperand(CurrentFunction, node.ChildNodes[node.ChildNodes.FindIndex(e => e.Term.Name == "expr")], 0);
            AddEvalLocal(CurrentFunction, ArrayName, true);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

            AddEvalLocal(CurrentFunction, ArrayName, false);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.FirstArrayKey));
            AddEvalLocal(CurrentFunction, KeyName, true);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

            EnterLoop(CurrentFunction);

            dynamic __header = CurrentFunction.Locals.GetEndOfChain();

            AddEvalLocal(CurrentFunction, KeyName, false);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.IsDefined));

            dynamic __jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnFalse));

            AddEvalLocal(CurrentFunction, KeyName, false);
            AddEvalLocal(CurrentFunction, ArrayName, false);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.EvalArray));
            AddEvalLocal(CurrentFunction, ValueName, true);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

            yield return new QOperand(CurrentFunction, node.ChildNodes[node.ChildNodes.Count - 1], 0);

            dynamic __foreach_header = CurrentFunction.Locals.GetEndOfChain();

            AddEvalLocal(CurrentFunction, KeyName, false);
            AddEvalLocal(CurrentFunction, ArrayName, false);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.NextArrayKey));
            AddEvalLocal(CurrentFunction, KeyName, true);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

            dynamic __footer = CurrentFunction.AddJump(DynOp(ScriptOpCode.Jump));
            __footer.After = __header;
            __jmp.After = __footer;

            ExitLoop(CurrentFunction, __foreach_header, __footer);
        }

        private IEnumerable<QOperand> EmitEvalFieldVariable(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            foreach (var val in EmitObject(CurrentFunction, node.ChildNodes[0].ChildNodes[0], Context | (uint)ScriptContext.IsRef))
            {
                yield return val;
            }

            AddFieldVariable(CurrentFunction, node.ChildNodes[1].FindTokenAndGetText(), Context);
        }

        private void AddFieldVariable(dynamic CurrentFunction, string FVIdentifier, uint Context)
        {
            (CurrentFunction as T7ScriptExport).AddFieldVariable(T7().ScriptHash(FVIdentifier.ToLower()), Context);
        }

        private IEnumerable<QOperand> EmitShortArray(dynamic CurrentFunction, KeyValuePair<ParseTreeNode, ParseTreeNode> shRef, uint Context)
        {
            ParseTreeNode ShortHand = shRef.Key;
            ParseTreeNode Setter = shRef.Value;

            for (int i = 0; i < ShortHand.ChildNodes.Count; i++)
            {
                yield return new QOperand(CurrentFunction, ShortHand.ChildNodes[i], 0);
                CurrentFunction.AddGetNumber(i);

                yield return new QOperand(CurrentFunction, Setter, Context | (uint)ScriptContext.IsRef);

                CurrentFunction.AddOp(DynOp(ScriptOpCode.EvalArrayRef));
                CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));
            }
        }

        private IEnumerable<QOperand> EmitSVFOrShorthand(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            foreach (var val in EmitSetVariableField(CurrentFunction, node, 0))
            {
                yield return val;
            }

            KeyValuePair<ParseTreeNode, ParseTreeNode> shRef = ArrayContext;

            if (shRef.Key != null)
                foreach (var val in EmitShortArray(CurrentFunction, shRef, 0))
                    yield return val;
        }

        private IEnumerable<QOperand> EmitReturn(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            if (node.ChildNodes.Count > 1)
            {
                yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
                CurrentFunction.AddOp(DynOp(ScriptOpCode.Return));
            }
            else
            {
                CurrentFunction.AddOp(DynOp(ScriptOpCode.End));
            }
        }

        private IEnumerable<QOperand> EmitWait(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.Wait));
        }

        private IEnumerable<QOperand> EmitCall(dynamic CurrentFunction, ParseTreeNode callNode, uint Context)
        {
            //We will receive a Call term, which consists of a CallFrame and an optional CallPrefix

            ParseTreeNode CallFrame = callNode.ChildNodes[callNode.ChildNodes.Count - 1];
            ParseTreeNode BaseCall = CallFrame.ChildNodes[0];
            ParseTreeNode CallPrefix = callNode.ChildNodes.Count > 1 ? callNode.ChildNodes[0] : null;
            ParseTreeNode Caller = null;

            string function_name = BaseCall.ChildNodes[BaseCall.ChildNodes.Count - 2].FindTokenAndGetText().ToLower();

            string NS_String = null;
            uint fhash = Script.ScriptHash(function_name);

            if (BaseCall.ChildNodes[0].Term.Name == "gscForFunction")
                NS_String = NormalizeUsing(BaseCall.ChildNodes[0].ChildNodes[0].FindTokenAndGetText()).Trim();

            ParseTreeNode CallParameters = BaseCall.ChildNodes[BaseCall.ChildNodes.Count - 1].ChildNodes[0];
            ParseTreeNodeList parameters = CallParameters.ChildNodes;

            //Our context should update if we have a prefix
            if (CallPrefix != null)
            {
                if (CallPrefix.ChildNodes[0].Term.Name == "expr")
                {
                    Context |= (uint)ScriptContext.HasCaller;
                    Caller = CallPrefix.ChildNodes[0];
                }

                if (CallPrefix.ChildNodes[CallPrefix.ChildNodes.Count - 1].Term.Name == "thread")
                    Context |= (uint)ScriptContext.Threaded;
            }

            //Update the context if we are using a call pointer term
            if (CallFrame.ChildNodes[0].Term.Name == CALL_PTR_TERMNAME)
                Context |= (uint)ScriptContext.IsPointer;

            //Checking builtins first
            if (!HasContext(Context, ScriptContext.IsPointer) && !HasContext(Context, ScriptContext.Threaded) && NS_String == null)
            {
                //Builtin methods can be resolved here because they have specific emission patterns per function
                if (IsBuiltinMethod(function_name))
                {
                    dynamic result;

                    if (HasContext(Context, ScriptContext.HasCaller))
                    {
                        foreach (var val in EmitNotifierCall(CurrentFunction, CallPrefix, BaseCall))
                        {
                            yield return val;
                        }
                        result = ScriptOperands.Pop().ObjectValue;
                    }
                    else
                    {
                        parameters.Reverse();
                        foreach (ParseTreeNode parameter in parameters)
                        {
                            yield return new QOperand(CurrentFunction, parameter, 0);
                        }

                        result = CurrentFunction.TryAddBuiltInCall(BaseCall.ChildNodes[0].Token.ValueString.ToLower());
                    }

                    if (result != null)
                        yield break;

                    throw new NotImplementedException($"Call to builtin method '{BaseCall.ChildNodes[0].Token.ValueString}' has not been handled!");
                }
            }

            //All calls need parameters!
            CurrentFunction.AddOp(DynOp(ScriptOpCode.PreScriptCall));

            bool DoProtect = EnableStatPtrProtect && !HasContext(Context, ScriptContext.IsCustomInject) && T7().IsStatProtected(fhash);

            parameters.Reverse();
            foreach (ParseTreeNode parameter in parameters)
            {
                yield return new QOperand(CurrentFunction, parameter, 0);
            }

            if (T7().Imports.IsBuiltinImport(fhash))
                (CurrentFunction as T7ScriptExport).AddGetNumber((int)fhash);

            if (HasContext(Context, ScriptContext.HasCaller))
                yield return new QOperand(CurrentFunction, Caller, 0);

            if (HasContext(Context, ScriptContext.IsPointer))
            {
                yield return new QOperand(CurrentFunction, CallFrame.ChildNodes[0].ChildNodes[0], 0);

                CurrentFunction.AddCallPtr(Context, (byte)parameters.Count);
            }
            else
            {
                uint t7_ns = ScriptNamespace; //newer games automatically figure out you want a builtin if its the same namespace

                if (NS_String != null)
                    t7_ns = Script.ScriptHash(NS_String);

                byte Flags = 0;

                if (HasContext(Context, ScriptContext.Threaded))
                    Flags |= (byte)ImportFlags.IsRef;

                if (HasContext(Context, ScriptContext.HasCaller))
                    Flags |= (byte)ImportFlags.IsMethod;
                else
                    Flags |= (byte)ImportFlags.IsFunction;

                if (t7_ns == ScriptNamespace)
                    Flags |= (byte)ImportFlags.NeedsResolver;

                // will work for either game
                if (T7Import.DevFunctions.Contains(function_name) && t7_ns == ScriptNamespace)
                    Flags |= (byte)ImportFlags.IsDebug;

                dynamic ImportRef = null;

                ImportRef = Script.Imports.AddImport(fhash, t7_ns, (byte)parameters.Count, Flags);

                CurrentFunction.AddCall(ImportRef, Context);
            }

            if (HasContext(Context, ScriptContext.DecTop))
                CurrentFunction.AddOp(DynOp(ScriptOpCode.DecTop));
        }

        private IEnumerable<QOperand> EmitNotifierCall(dynamic CurrentFunction, ParseTreeNode CallPrefix, ParseTreeNode BaseCall)
        {
            ParseTreeNode CallParameters = BaseCall.ChildNodes[1].ChildNodes[0];
            ParseTreeNodeList parameters = CallParameters.ChildNodes;

            switch (BaseCall.ChildNodes[0].Token.ValueString.ToLower())
            {
                case "waittillmatch":
                case "waittill":

                    yield return new QOperand(CurrentFunction, parameters[0], 0);
                    foreach (var val in EmitObject(CurrentFunction, CallPrefix, 0))
                        yield return val;

                    CurrentFunction.AddOp(DynOp(ScriptOpCode.WaitTill));

                    for (int i = 1; i < parameters.Count; i++)
                    {
                        yield return new QOperand(CurrentFunction, parameters[i], (uint)ScriptContext.Waittill);
                    }

                    PushObject(CurrentFunction.AddOp(DynOp(ScriptOpCode.ClearParams)));
                    yield break;

                case "notify":

                    parameters.Reverse();
                    CurrentFunction.AddOp(DynOp(ScriptOpCode.PreScriptCall));

                    foreach (ParseTreeNode parameter in parameters)
                    {
                        yield return new QOperand(CurrentFunction, parameter, 0);
                    }

                    foreach (var val in EmitObject(CurrentFunction, CallPrefix, 0))
                        yield return val;

                    PushObject(CurrentFunction.AddOp(DynOp(ScriptOpCode.Notify)));
                    yield break;

                case "endon":

                    parameters.Reverse();

                    foreach (ParseTreeNode parameter in parameters)
                        yield return new QOperand(CurrentFunction, parameter, 0);

                    foreach (var val in EmitObject(CurrentFunction, CallPrefix, 0))
                        yield return val;

                    PushObject(CurrentFunction.AddOp(DynOp(ScriptOpCode.EndOn)));
                    yield break;
            }

            throw new ArgumentException($"{BaseCall.ChildNodes[0].Token.ValueString.ToLower()} was passed to EmitNotifierCall, but isnt a valid notfier");
        }

        private IEnumerable<QOperand> EmitObject(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            if (node.Token != null)
            {
                dynamic op = CurrentFunction.TryAddBuiltIn(node.Token.ValueString, HasContext(Context, ScriptContext.IsRef));
                if (op != null)
                    yield break;
            }

            yield return new QOperand(CurrentFunction, node, 0);

            if (HasContext(Context, ScriptContext.IsRef))
                CurrentFunction.AddOp(DynOp(ScriptOpCode.CastFieldObject));
        }

        private bool IsBuiltinMethod(string identifier)
        {
            return T7ScriptExport.IsBuiltinMethod(identifier);
        }

        private IEnumerable<QOperand> EmitSwitchStatement(dynamic CurrentFunction, ParseTreeNode node)
        {
            if (!CurrentFunction.TryPopSwitchKey(out string SWKey))
                throw new InvalidOperationException("Tried to compile more switch statements than were expected");

            ParseTreeNodeList SwitchContentsArray = node.ChildNodes[2].ChildNodes;

            EnterLoop(CurrentFunction);

            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            AddEvalLocal(CurrentFunction, SWKey, true);

            CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

            ParseTreeNode DefaultNode = null;

            foreach (var _node in SwitchContentsArray)
            {
                if (_node.ChildNodes[0].ChildNodes[0].Term.Name.ToLower() == "default")
                {
                    DefaultNode = _node;
                    break;
                }
            }

            if (DefaultNode != null)
            {
                SwitchContentsArray.Remove(DefaultNode);
                SwitchContentsArray.Add(DefaultNode);
            }

            List<dynamic> __orjumps = new List<dynamic>();

            foreach (var _node in SwitchContentsArray)
            {
                if (_node == DefaultNode)
                {
                    foreach (var jmp in __orjumps)
                        jmp.After = CurrentFunction.Locals.GetEndOfChain();

                    __orjumps.Clear();

                    if (DefaultNode.ChildNodes.Count > 1)
                        yield return new QOperand(CurrentFunction, DefaultNode.ChildNodes[1], 0);

                    break;
                }

                yield return new QOperand(CurrentFunction, _node.ChildNodes[0].ChildNodes[1], 0);
                AddEvalLocal(CurrentFunction, SWKey, false);
                CurrentFunction.AddCompareOp("==");


                if (_node.ChildNodes.Count > 1)
                {
                    dynamic __jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnFalse));

                    foreach (var jmp in __orjumps)
                        jmp.After = CurrentFunction.Locals.GetEndOfChain();

                    __orjumps.Clear();

                    yield return new QOperand(CurrentFunction, _node.ChildNodes[1], 0);

                    __jmp.After = CurrentFunction.Locals.GetEndOfChain();
                }
                else
                {
                    dynamic __jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnTrue));
                    __orjumps.Add(__jmp);
                }

            }

            ExitLoop(CurrentFunction, null, CurrentFunction.Locals.GetEndOfChain());
        }

        private KeyValuePair<ParseTreeNode, ParseTreeNode> ArrayContext;
        private IEnumerable<QOperand> EmitSetVariableField(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            ParseTreeNode shNode = null;
            ArrayContext = default;

            if (node.ChildNodes[1].ChildNodes[0].Term.Name != "=" && node.ChildNodes.Count > 2)
            {
                yield return new QOperand(CurrentFunction, node.ChildNodes[0].ChildNodes[0], 0);
                yield return new QOperand(CurrentFunction, node.ChildNodes[2].ChildNodes[0], 0);
            }

            switch (node.ChildNodes[1].ChildNodes[0].Term.Name)
            {
                case "++":
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context | (uint)ScriptContext.IsRef);
                    CurrentFunction.AddOp(DynOp(ScriptOpCode.Inc));
                    yield break;

                case "--":
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context | (uint)ScriptContext.IsRef);
                    CurrentFunction.AddOp(DynOp(ScriptOpCode.Dec));
                    yield break;

                case "=":
                    if (node.ChildNodes[2].ChildNodes[0].Term.Name.ToLower() == "shorthandarray") //set the variable to an empty array
                    {
                        shNode = node.ChildNodes[2].ChildNodes[0].ChildNodes[0]; //set a reference to the array for the array handler
                        CurrentFunction.AddOp(DynOp(ScriptOpCode.GetEmptyArray));
                    }
                    else
                    {
                        yield return new QOperand(CurrentFunction, node.ChildNodes[2].ChildNodes[0], 0);
                    }
                    break;

                default:
                    CurrentFunction.AddMathToken(node.ChildNodes[1].ChildNodes[0].Term.Name[0].ToString());
                    break;

            }

            yield return new QOperand(CurrentFunction, node.ChildNodes[0].ChildNodes[0], Context | (uint)ScriptContext.IsRef);
            CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

            ArrayContext = new KeyValuePair<ParseTreeNode, ParseTreeNode>(shNode, node.ChildNodes[0].ChildNodes[0]);
        }

        private IEnumerable<QOperand> EmitForLoop(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            EnterLoop(CurrentFunction);
            ParseTreeNode Header = node.ChildNodes[1];

            int SetVarIndex = Header.ChildNodes.FindIndex(e => e.Term.Name == "setVariableField");
            int BoolExprIndex = Header.ChildNodes.FindIndex(e => e.Term.Name == "booleanExpression");
            int IterateIndex = Header.ChildNodes.FindIndex(e => e.Term.Name == "forIterate");

            if (SetVarIndex != -1)
                yield return new QOperand(CurrentFunction, Header.ChildNodes[SetVarIndex], 0);

            dynamic __header = CurrentFunction.Locals.GetEndOfChain();
            dynamic __jmp = null;

            if (BoolExprIndex != -1)
            {
                foreach (var val in EmitBoolExpr(CurrentFunction, Header.ChildNodes[BoolExprIndex], 0))
                {
                    yield return val;
                }
                __jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnFalse));
            }

            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);

            dynamic __ctheader = CurrentFunction.Locals.GetEndOfChain();

            if (IterateIndex != -1)
            {
                foreach (var val in EmitSetVariableField(CurrentFunction, Header.ChildNodes[IterateIndex], Context))
                {
                    yield return val;
                }
            }

            dynamic __bottomjump = CurrentFunction.AddJump(DynOp(ScriptOpCode.Jump));

            __bottomjump.After = __header;

            if (__jmp != null)
                __jmp.After = __bottomjump;

            ExitLoop(CurrentFunction, __ctheader, __bottomjump);
        }

        private void EnterLoop(dynamic CurrentFunction)
        {
            CurrentFunction.IncLCFContext();
        }

        private void ExitLoop(dynamic CurrentFunction, dynamic Header, dynamic Footer)
        {
            while (CurrentFunction.TryPopLCF(out T7OP_Jump __lcf))
            {
                __lcf.After = __lcf.RefHead ? Header : Footer;
            }

            CurrentFunction.DecLCFContext();
        }

        private IEnumerable<QOperand> EmitWhile(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            EnterLoop(CurrentFunction);

            dynamic __backref = CurrentFunction.Locals.GetEndOfChain();

            foreach (var v in EmitBoolExpr(CurrentFunction, node.ChildNodes[1], 0))
                yield return v;

            dynamic __while_jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnFalse));

            yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
            dynamic __while_jmp_back = CurrentFunction.AddJump(DynOp(ScriptOpCode.Jump));

            __while_jmp.After = __while_jmp_back;
            __while_jmp_back.After = __backref;

            ExitLoop(CurrentFunction, __backref, __while_jmp_back);
        }

        private IEnumerable<QOperand> EmitConditionalJump(dynamic CurrentFunction, ParseTreeNode BoolExpr, ParseTreeNode BlockContent, ParseTreeNode SecondBlock = null)
        {
            foreach (var entry in EmitBoolExpr(CurrentFunction, BoolExpr, 0))
            {
                yield return entry;
            }

            dynamic __if_jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnFalse));
            yield return new QOperand(CurrentFunction, BlockContent, 0);

            if (SecondBlock != null)
            {
                dynamic __else_jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.Jump));
                __if_jmp.After = CurrentFunction.Locals.GetEndOfChain();

                yield return new QOperand(CurrentFunction, SecondBlock, 0);
                __else_jmp.After = CurrentFunction.Locals.GetEndOfChain();
            }
            else
            {
                __if_jmp.After = CurrentFunction.Locals.GetEndOfChain();
            }
        }

        private IEnumerable<QOperand> EmitBoolExpr(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            switch (node.ChildNodes.Count)
            {
                case 0:
                    yield break;

                case 1:
                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context);
                    yield break;

                case 3:

                    yield return new QOperand(CurrentFunction, node.ChildNodes[0], 0);
                    dynamic target = node.ChildNodes[1].Term.Name == "&&" ? DynOp(ScriptOpCode.JumpOnFalseExpr) : DynOp(ScriptOpCode.JumpOnTrueExpr);
                    dynamic __jmp = CurrentFunction.AddJump(target);
                    yield return new QOperand(CurrentFunction, node.ChildNodes[2], 0);
                    __jmp.After = CurrentFunction.Locals.GetEndOfChain();

                    yield break;

                default:
                    throw new NotImplementedException($"Boolean expression contained an unhandled number of childnodes ({node.ChildNodes.Count})");
            }
        }

        private IEnumerable<QOperand> EmitArray(QOperand CurrentOp)
        {
            var node = CurrentOp.ObjectNode;
            var CurrentFunction = CurrentOp.CurrentFunction;
            var Context = CurrentOp.Context;

            yield return new QOperand(CurrentFunction, node.ChildNodes[1], 0);
            yield return new QOperand(CurrentFunction, node.ChildNodes[0], Context);
            CurrentFunction.AddOp(HasContext(Context, ScriptContext.IsRef) ? DynOp(ScriptOpCode.EvalArrayRef) : DynOp(ScriptOpCode.EvalArray));
        }

        private bool HasContext(uint context, ScriptContext desired)
        {
            return (context & (uint)desired) > 0;
        }

        private void EmitOptionalParameters(dynamic CurrentFunction, ParseTreeNodeList Params, bool IsCustomInject)
        {
            foreach (var node in Params)
            {
                if (node.ChildNodes[0].Term.Name != "setOptionalParam")
                    continue;

                var optional = node.ChildNodes[0];
                string pname = optional.ChildNodes[0].FindTokenAndGetText().ToLower();

                AddEvalLocal(CurrentFunction, pname, false);
                CurrentFunction.AddOp(DynOp(ScriptOpCode.IsDefined));

                dynamic __jmp = CurrentFunction.AddJump(DynOp(ScriptOpCode.JumpOnTrue));

                Push(CurrentFunction, optional.ChildNodes[2], 0);
                IterateStack(IsCustomInject);

                AddEvalLocal(CurrentFunction, pname, true);
                CurrentFunction.AddOp(DynOp(ScriptOpCode.SetVariableField));

                __jmp.After = CurrentFunction.Locals.GetEndOfChain();
            }
        }

        private void Push(QOperand op)
        {
            ScriptOperands.Push(op);
        }

        private void Push(dynamic CurrentFunction, ParseTreeNode node, uint Context)
        {
            ScriptOperands.Push(new QOperand(CurrentFunction, node, Context));
        }

        private void PushObject(object o)
        {
            ScriptOperands.Push(new QOperand(null, o, 0));
        }

        private dynamic DynOp(dynamic opcode)
        {
            return (T7CompilerLib.OpCodes.ScriptOpCode)opcode;
        }

        private void AddEvalLocal(dynamic CurrentFunction, string pname, bool IsRef, bool HasWaittillContext = false)
        {
            uint phash = Script.ScriptHash(pname);
            (CurrentFunction as T7ScriptExport).AddEvalLocal(pname, phash, IsRef, HasWaittillContext);
        }

        private IEnumerable<string> CollectLocalVariables(dynamic CurrentFunction, ParseTreeNode node, bool AllowIDCollection)
        {
            Stack<ParseTreeNode> Remaining = new Stack<ParseTreeNode>();
            Remaining.Push(node);

            while (Remaining.Count > 0)
            {
                node = Remaining.Pop();

                foreach (var childNode in node.ChildNodes)
                {
                    switch (childNode.Term.Name)
                    {
                        case "identifier":
                            if (AllowIDCollection)
                                yield return childNode.FindTokenAndGetText().ToLower();
                            break;

                        case "setVariableField":
                            if (childNode.ChildNodes[0].ChildNodes[0].Term.Name.Contains("identifier"))
                            {
                                yield return childNode.FindTokenAndGetText().ToLower();
                            }
                            break;

                        case "foreachSingle":
                        case "foreachDouble":
                            //This is needed for nested loops, These are temps values to help the compiler (needed)

                            var first = (Helpers.BogsArrayKeys + Guid.NewGuid()).ToLower(); //Array Keys
                            var second = (Helpers.BogsArrayIndex + Guid.NewGuid()).ToLower(); //Index

                            CurrentFunction.PushFEPair(new KeyValuePair<string, string>(first, second));

                            yield return first;
                            yield return second;

                            if (childNode.Term.Name == "foreachDouble")
                                yield return childNode.ChildNodes[childNode.ChildNodes.FindIndex(e => e.Term.Name.ToLower() == "key")].FindTokenAndGetText().ToLower();
                            yield return childNode.ChildNodes[childNode.ChildNodes.FindIndex(e => e.Term.Name.ToLower() == "value")].FindTokenAndGetText().ToLower();
                            break;

                        case "switchStatement":

                            var key = (Helpers.BogsArrayKeys + Guid.NewGuid()).ToLower();

                            CurrentFunction.PushSwitchKey(key);

                            yield return key;

                            break;

                        case "callFrame":
                            if (childNode.ChildNodes[0].ChildNodes[0].FindTokenAndGetText() == "waittill")
                            {
                                var _params = childNode.ChildNodes[0].ChildNodes[1].ChildNodes[0].ChildNodes;
                                for (int i = 1; i < _params.Count; i++)
                                {
                                    foreach (string s in CollectLocalVariables(CurrentFunction, _params[i], true))
                                        yield return s;
                                }
                            }
                            break;
                    }
                    Remaining.Push(childNode);
                }
            }
            yield break;
        }

        private void AddLocal(dynamic CurrentFunction, string LocalName)
        {
            (CurrentFunction as T7ScriptExport).Locals.AddLocal(T7().ScriptHash(LocalName));
        }

        private dynamic CreateFunction(ParseTreeNode functionNode, string FunctionName)
        {
            return T7().Exports.Add(FunctionMetadata[FunctionName].FunctionHash, FunctionMetadata[FunctionName].NamespaceHash, FunctionMetadata[FunctionName].NumParams);
        }

        private string NormalizeUsing(string include)
        {
            include = include.ToLower();
            return include;
        }

        private void SetNamespace()
        {
            if (_tree.Root.ChildNodes[0].ChildNodes.Count > 0)
            {
                var directive = _tree.Root.ChildNodes[0].ChildNodes[0].ChildNodes.Find(x => x.ChildNodes[0].Term.Name.ToLower() == "namespace");
                if (directive != null)
                    ScriptNamespace = Script.ScriptHash(directive.ChildNodes[0].ChildNodes[1].FindTokenAndGetText().ToLower());
            }
        }

        private short[] GetOpCodeArray()
        {

            var randomizer = new List<short>();
            foreach (var entry in Script.Header.OpcodeValues)
            {
                randomizer.Add((short)entry.Value);//Fake
                randomizer.Add((short)T7().ScriptMetadata[(ScriptOpCode)entry.Key]);
            }

            return randomizer.ToArray();
        }

        private struct ScriptFunctionMetaData
        {
            public uint FunctionHash;
            public uint NamespaceHash;
            public string FunctionName;
            public string NamespaceName;
            public byte NumParams;
            public byte Flags;
        }

        private class QOperand
        {
            public readonly bool IsParseNode;
            public object ObjectValue { private set; get; }
            public ParseTreeNode ObjectNode
            {
                get
                {
                    return ObjectValue as ParseTreeNode;
                }
            }

            private IEnumerable<QOperand> __operandsList;
            public IEnumerable<QOperand> SetOperands
            {
                set
                {
                    __operandsList = value;
                    GetOperands = __operandsList.GetEnumerator();
                }
            }

            public IEnumerator<QOperand> GetOperands { get; private set; }

            public readonly dynamic CurrentFunction;
            public readonly uint Context;

            public QOperand(dynamic export, object Value, uint context)
            {
                if (Value is ParseTreeNode)
                    IsParseNode = true;

                ObjectValue = Value;
                CurrentFunction = export;
                Context = context;
            }

            public QOperand Replace(int index)
            {
                ObjectValue = ObjectNode.ChildNodes[index];

                return this;
            }
        }
    }
}
