using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;

namespace HKDebug.HotReload
{
    public static class HRType
    {
        
        static readonly MethodInfo HStartPushArg = typeof(HRHook).GetMethod("HStartPushArg");
        static readonly MethodInfo HPushArgs = typeof(HRHook).GetMethod("HPushArgs");
        static readonly MethodInfo HEndPushArg = typeof(HRHook).GetMethod("HEndPushArg");
        static readonly MethodInfo GType = typeof(HRHook).GetMethod("GetType");
        static readonly MethodInfo InvokeMethod = typeof(HRHook).GetMethod("InvokeMethod");
        static readonly MethodInfo GFI = typeof(HRHook).GetMethod("GetInstanceField");
        static readonly MethodInfo SFI = typeof(HRHook).GetMethod("SetInstanceField");
        internal static void MethodConvert(MethodInfo m)
        {
            HookEndpointManager.Modify(m, new Action<ILContext>((il) =>
             {
                 ILCursor cur = new ILCursor(il);
                 Instruction now = null;
                 Instruction next = il.Instrs[0];
                 while ((now = next) != null)
                 {
                     next = now.Next;
                     OpCode op = now.OpCode;
                     if (op == OpCodes.Call || op == OpCodes.Callvirt)
                     {
                         HCall(il, il.IL, now);
                     }
                     else if (op == OpCodes.Ldfld)
                     {
                         FieldReference fr = (FieldReference)now.Operand;
                         cur.Goto(now);
                         cur.Emit(OpCodes.Ldstr, fr.Name);
                         cur.Emit(OpCodes.Ldstr, fr.DeclaringType.FullName);
                         cur.Emit(OpCodes.Call, GType);
                         cur.Emit(OpCodes.Call, GFI);

                         Nop(now);
                     }
                     else if (op == OpCodes.Stfld)
                     {
                         FieldReference fr = (FieldReference)now.Operand;
                         cur.Goto(now);
                         cur.Emit(OpCodes.Ldstr, fr.Name);
                         cur.Emit(OpCodes.Ldstr, fr.DeclaringType.FullName);
                         cur.Emit(OpCodes.Call, GType);
                         cur.Emit(OpCodes.Call, SFI);

                         Nop(now);
                     }
                     else if (op == OpCodes.Ldsfld)
                     {
                         FieldReference fr = (FieldReference)now.Operand;
                         cur.Goto(now);
                         
                         cur.Emit(OpCodes.Ldstr, fr.Name);
                         cur.Emit(OpCodes.Ldstr, fr.DeclaringType.FullName);
                         cur.Emit(OpCodes.Call, GType);
                         cur.EmitDelegate<Action<string, Type>>((a, b) => HRHook.GetField(null, a, b));

                         Nop(now);
                     }
                     else if (op == OpCodes.Stsfld)
                     {
                         FieldReference fr = (FieldReference)now.Operand;
                         
                         cur.Goto(now);
                         cur.Emit(OpCodes.Ldstr, fr.Name);
                         cur.Emit(OpCodes.Ldstr, fr.DeclaringType.FullName);
                         cur.Emit(OpCodes.Call, GType);
                         cur.EmitDelegate<Action<object, string, Type>>((a, b, c) => HRHook.SetField(null, a, b, c));

                         Nop(now);
                     }
                     else if(op == OpCodes.Castclass)
                     {
                         TypeReference tr = (TypeReference)now.Operand;
                         cur.Goto(now);

                         Nop(now);
                     }
                 }
             }));
        }
        internal static void Nop(Instruction ins)
        {
            ins.OpCode = OpCodes.Nop;
            ins.Operand = null;
        }
        internal static void HCall(ILContext ct, ILProcessor il, Instruction ins)
        {
            MethodReference mr = (MethodReference)ins.Operand;

            int pc = mr.Parameters.Count;
            ILCursor cur = new ILCursor(ct);
            cur.Goto(ins);

            //Push Args
            cur.Emit(OpCodes.Call, HStartPushArg);
            for (int i = 0; i < pc; i++)
            {
                cur.Emit(OpCodes.Call, HPushArgs);
            }
            cur.Emit(OpCodes.Call, HEndPushArg);
            //Type
            cur.Emit(OpCodes.Ldstr, mr.DeclaringType.FullName);
            cur.Emit(OpCodes.Call, GType);
            //Name
            cur.Emit(OpCodes.Ldstr, mr.Name);
            //P Type
            cur.Emit(OpCodes.Newarr, typeof(Type[]));
            for(int i = 0; i < pc; i++)
            {
                cur.Emit(OpCodes.Ldc_I4, i);
                cur.Emit(OpCodes.Ldstr, mr.Parameters[i].ParameterType.FullName);
                cur.Emit(OpCodes.Call, GType);
                cur.Emit(OpCodes.Stelem_Any, typeof(Type));
            }
            cur.Emit(OpCodes.Call, InvokeMethod);
            Nop(ins);
        }
    }
}
