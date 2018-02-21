using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.ObjectPool
{
    internal ref struct PolicyCompiler<T>
    {
        public Func<T> CompileCreate(object owner, IPooledObjectPolicy<T> policy, string policyFieldName)
        {
            var (ownerType, policyField) = GetOwnerInfo(owner, policyFieldName);
            var target = policy.GetType();
            var createMethod = target.GetMethod(nameof(IPooledObjectPolicy<T>.Create));
            var dm = new DynamicMethod("_Create_", typeof(T), new[] { ownerType }, ownerType);
            var ilGen = dm.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, policyField);
            ilGen.Emit(OpCodes.Call, createMethod);
            ilGen.Emit(OpCodes.Ret);

            return dm.CreateDelegate(typeof(Func<T>), owner) as Func<T>;
        }

        public Func<T, bool> CompileReturn(object owner, IPooledObjectPolicy<T> policy, string policyFieldName)
        {
            var (ownerType, policyField) = GetOwnerInfo(owner, policyFieldName);
            var target = policy.GetType();
            var returnMethod = target.GetMethod(nameof(IPooledObjectPolicy<T>.Return));
            var dm = new DynamicMethod("_Return_", typeof(bool), new[] { ownerType, typeof(T) }, ownerType);
            var ilGen = dm.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, policyField);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Call, returnMethod);
            ilGen.Emit(OpCodes.Ret);

            return dm.CreateDelegate(typeof(Func<T, bool>), owner) as Func<T, bool>;
        }

        private (Type ownerType, FieldInfo policyField) GetOwnerInfo(object owner, string policyFieldName)
        {
            var ownerType = owner.GetType();
            var policyField = ownerType.GetField(policyFieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            return (ownerType, policyField);
        }
    }
}
