﻿using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection;

namespace EazFixer.Processors
{
    internal class StringFixer : ProcessorBase
    {
        private MethodDef _decrypterMethod;
        public static readonly BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.GetProperty;
        

        protected override void InitializeInternal()
        {
          
        }

        protected override void ProcessInternal()
        {
            //a dictionary to cache all strings
            var dictionary = new Dictionary<int, string>();
            Type classes = Ctx.Assembly.GetType(Flags.type);
            MethodInfo decrypter = classes.GetMethod(Flags.method, all);

           

            //store it so we can use it in the stacktrace patch
            StacktracePatcher.PatchStackTraceGetMethod.MethodToReplace = decrypter;

            //for every method with a body...
            foreach (MethodDef meth in Utils.GetMethodsRecursive(Ctx.Module).Where(a => a.HasBody && a.Body.HasInstructions))
            {
                //.. and every instruction (starting at the second one) ...
                for (int i = 1; i < meth.Body.Instructions.Count; i++)
                {
                    //get this instruction and the previous
                    var prev = meth.Body.Instructions[i - 1];
                    var curr = meth.Body.Instructions[i];

                    //if they invoke the string decrypter method with an int parameter
                    if (prev.IsLdcI4() && curr.Operand != null && curr.Operand is MethodDef md && curr.Operand.ToString().Contains(decrypter.Name))
                    {
                        //get the int parameter, and get the resulting string from either cache or invoking the decrypter method
                        int val = prev.GetLdcI4Value();
                        if (!dictionary.ContainsKey(val))
                            dictionary[val] = (string) decrypter.Invoke(null, new object[] {val});
                            
                        // check if str == .ctor due to eaz using string decryptor to call constructors
                        if (dictionary[val] == ".ctor" && Flags.VirtFix) continue;

                        //replace the instructions with the string
                        prev.OpCode = OpCodes.Nop;
                        curr.OpCode = OpCodes.Ldstr;
                        curr.Operand = dictionary[val];
                    }
                }
            }
        }

        protected override void CleanupInternal()
        {
            //check if virtfix is active so ignore cleaning
            if (Flags.VirtFix)
                throw new Exception("VirtFix enabled, Cannot remove method");

            //ensure that the string decryptor isn't called anywhere
            if (Utils.LookForReferences(Ctx.Module, _decrypterMethod))
                throw new Exception("String decrypter is still being called");

            //remove the string decryptor class
            var stringType = _decrypterMethod.DeclaringType;
            Ctx.Module.Types.Remove(stringType);
        }
    }
}
