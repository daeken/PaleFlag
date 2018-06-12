using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HypervisorSharp;

namespace PaleFlag.XboxKernel {
	class ExportAttribute : Attribute {
		public readonly int Number;

		public ExportAttribute(int number) => Number = number;
	}
	
	public partial class Kernel {
		readonly CpuCore Cpu;
		public static readonly Dictionary<int, Action> Functions = new Dictionary<int, Action>();
		
		public Kernel(CpuCore cpu) {
			Cpu = cpu;
			foreach(var method in typeof(Kernel).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
				var attr = method.GetCustomAttribute<ExportAttribute>();
				if(attr == null)
					continue;
				Functions[attr.Number] = MakeWrapper(method);
			}
		}
		
		Action MakeWrapper(MethodInfo method) {
			var toStructure = typeof(Marshal).GetMethod("PtrToStructure", new[] { typeof(IntPtr) });
			var paramBuilders = method.GetParameters().Select<ParameterInfo, Func<object>>(param => {
				var spec = toStructure.MakeGenericMethod(param.ParameterType);
				Func<IntPtr, object> caller = ptr => spec.Invoke(null, new object[] { ptr });
				if(param.ParameterType.IsGenericType &&
				   param.ParameterType.GetGenericTypeDefinition() == typeof(GuestMemory<>))
					caller = ptr => 
						Activator.CreateInstance(typeof(GuestMemory<>).MakeGenericType(param.ParameterType.GenericTypeArguments[0]), 
							Marshal.PtrToStructure<uint>(ptr));

				var size = Extensions.SizeOf(param.ParameterType);
				return () => {
					var sp = Cpu[HvReg.RSP];
					var data = PageManager.Instance.Read(sp, size);
					var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
					var ret = caller(handle.AddrOfPinnedObject());
					handle.Free();
					Cpu[HvReg.RSP] = sp + (uint) size;
					return ret;
				};
			}).ToArray();
			var numParams = paramBuilders.Length;
			var iparams = new object[numParams];
			if(method.ReturnType == typeof(void))
				return () => {
					var sp = Cpu[HvReg.RSP];
					Cpu[HvReg.RSP] = sp + 4;
					var retAddr = new GuestMemory<uint>(sp).Value;
					for(var i = 0; i < numParams; ++i)
						iparams[i] = paramBuilders[i]();
					method.Invoke(this, iparams);
					Cpu[HvReg.RIP] = retAddr;
				};
			else {
				var retType = method.ReturnType;
				var size = Extensions.SizeOf(retType);
				Debug.Assert(size == 4);
				var ptr = Marshal.AllocHGlobal(size);
				var arr = new byte[size];
				return () => {
					var sp = Cpu[HvReg.RSP];
					Cpu[HvReg.RSP] = sp + 4;
					var retAddr = new GuestMemory<uint>(sp).Value;
					for(var i = 0; i < numParams; ++i)
						iparams[i] = paramBuilders[i]();
					var ret = method.Invoke(this, iparams);
					Marshal.StructureToPtr(ret.GetType().IsEnum ? Convert.ChangeType(ret, Enum.GetUnderlyingType(ret.GetType())) : ret, ptr, true);
					Marshal.Copy(ptr, arr, 0, size);
					Cpu[HvReg.RAX] = BitConverter.ToUInt32(arr, 0);
					Cpu[HvReg.RIP] = retAddr;
				};
			}
		}
	}
}