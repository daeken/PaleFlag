using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HypervisorSharp;
using MoreLinq;

namespace PaleFlag.XboxKernel {
	public enum Kobject {
		DpcObject = 0x13
	}

	public enum CallingConvention {
		Stdcall = 0, 
		Fastcall = 1, 
		Cdecl = 2
	}
	
	class ExportAttribute : Attribute {
		public readonly int Number;
		public readonly CallingConvention CallingConvention;

		public ExportAttribute(int number, CallingConvention _callingConvention = CallingConvention.Stdcall) {
			Number = number;
			CallingConvention = _callingConvention;
		}
	}
	
	public partial class Kernel {
		readonly Xbox Box;
		readonly CpuCore Cpu;
		public readonly Dictionary<int, Action> Functions = new Dictionary<int, Action>();
		
		public Kernel(Xbox box) {
			Box = box;
			Cpu = box.Cpu;
			foreach(var method in typeof(Kernel).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
				var attr = method.GetCustomAttribute<ExportAttribute>();
				if(attr != null)
					Functions[attr.Number] = MakeWrapper(method, attr.CallingConvention);
			}

			Console.WriteLine($"Implemented {Functions.Count}/366 kernel functions");
		}

		(Func<object>, Action<object>) MakePlainWrapper(ParameterInfo param, HvReg? reg = null) {
			var toStructure = typeof(Marshal).GetMethod("PtrToStructure", new[] { typeof(IntPtr) });
			var spec = toStructure.MakeGenericMethod(param.ParameterType);
			Func<IntPtr, object> caller = ptr => spec.Invoke(null, new object[] { ptr });
			if(param.ParameterType.IsGenericType &&
			   param.ParameterType.GetGenericTypeDefinition() == typeof(GuestMemory<>))
				caller = ptr => 
					Activator.CreateInstance(typeof(GuestMemory<>).MakeGenericType(param.ParameterType.GenericTypeArguments[0]), 
						Marshal.PtrToStructure<uint>(ptr));
			else if(param.ParameterType.IsEnum)
				caller = ptr => Enum.ToObject(param.ParameterType, Marshal.PtrToStructure(ptr, Enum.GetUnderlyingType(param.ParameterType)));

			var size = Extensions.SizeOf(param.ParameterType);
			var data = new byte[size < 4 ? 4 : size];
			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var handleAddr = handle.AddrOfPinnedObject();
			
			return (() => {
				if(reg == null) {
					var sp = Cpu[HvReg.RSP];
					PageManager.Instance.Read(sp, data);
					var ret = caller(handleAddr);
					Cpu[HvReg.RSP] = sp + (uint) (size == 4 || size == 8 ? size : 4);
					return ret;
				} else {
					Array.Copy(BitConverter.GetBytes(Cpu[reg.Value]), data, 4);
					return caller(handleAddr);
				}
			}, null);
		}
		
		(Func<object>, Action<object>) MakeInWrapper(ParameterInfo param, HvReg? reg = null) {
			var baseType = param.ParameterType.GetElementType();
			
			var toStructure = typeof(Marshal).GetMethod("PtrToStructure", new[] { typeof(IntPtr) });
			var spec = toStructure.MakeGenericMethod(baseType);
			Func<IntPtr, object> caller = tptr => spec.Invoke(null, new object[] { tptr });
			if(baseType.IsGenericType &&
			   baseType.GetGenericTypeDefinition() == typeof(GuestMemory<>))
				caller = tptr => 
					Activator.CreateInstance(typeof(GuestMemory<>).MakeGenericType(baseType.GenericTypeArguments[0]), 
						Marshal.PtrToStructure<uint>(tptr));
			else if(baseType.IsEnum)
				caller = tptr => Enum.ToObject(baseType, Marshal.PtrToStructure(tptr, Enum.GetUnderlyingType(baseType)));
			
			var size = Extensions.SizeOf(baseType);
			var data = new byte[size];
			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var handleAddr = handle.AddrOfPinnedObject();

			var pdata = new byte[4];
			var phandle = GCHandle.Alloc(pdata, GCHandleType.Pinned);
			var phandleAddr = phandle.AddrOfPinnedObject();
			
			return (() => {
				if(reg == null) {
					var sp = Cpu[HvReg.RSP];
					PageManager.Instance.Read(sp, pdata);
					var ptr = Marshal.PtrToStructure<uint>(phandleAddr);
					Cpu[HvReg.RSP] = sp + 4;
					PageManager.Instance.Read(ptr, data);
				} else {
					var ptr = Cpu[reg.Value];
					PageManager.Instance.Read(ptr, data);
				}
				return caller(handleAddr);
			}, null);
		}

		(Func<object>, Action<object>) MakeOutWrapper(ParameterInfo param, HvReg? reg = null) {
			uint ptr = 0;

			var baseType = param.ParameterType.GetElementType();
			var size = Extensions.SizeOf(baseType);
			var data = new byte[size];
			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var handleAddr = handle.AddrOfPinnedObject();

			var pdata = new byte[4];
			var phandle = GCHandle.Alloc(pdata, GCHandleType.Pinned);
			var phandleAddr = phandle.AddrOfPinnedObject();
			
			return (() => {
				if(reg == null) {
					var sp = Cpu[HvReg.RSP];
					PageManager.Instance.Read(sp, pdata);
					ptr = Marshal.PtrToStructure<uint>(phandleAddr);
					Cpu[HvReg.RSP] = sp + 4;
				} else
					ptr = Cpu[reg.Value];
				return null;
			}, value => {
				Marshal.StructureToPtr(value, handleAddr, true);
				PageManager.Instance.Write(ptr, data);
			});
		}

		(Func<object>, Action<object>) MakeRefWrapper(ParameterInfo param, HvReg? reg = null) {
			var baseType = param.ParameterType.GetElementType();
			
			var toStructure = typeof(Marshal).GetMethod("PtrToStructure", new[] { typeof(IntPtr) });
			var spec = toStructure.MakeGenericMethod(baseType);
			Func<IntPtr, object> caller = tptr => spec.Invoke(null, new object[] { tptr });
			if(baseType.IsGenericType &&
			   baseType.GetGenericTypeDefinition() == typeof(GuestMemory<>))
				caller = tptr => 
					Activator.CreateInstance(typeof(GuestMemory<>).MakeGenericType(baseType.GenericTypeArguments[0]), 
						Marshal.PtrToStructure<uint>(tptr));
			else if(baseType.IsEnum)
				caller = tptr => Enum.ToObject(baseType, Marshal.PtrToStructure(tptr, Enum.GetUnderlyingType(baseType)));
			
			uint ptr = 0;

			var size = Extensions.SizeOf(baseType);
			var data = new byte[size];
			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			var handleAddr = handle.AddrOfPinnedObject();

			var pdata = new byte[4];
			var phandle = GCHandle.Alloc(pdata, GCHandleType.Pinned);
			var phandleAddr = phandle.AddrOfPinnedObject();
			
			return (() => {
				if(reg == null) {
					var sp = Cpu[HvReg.RSP];
					PageManager.Instance.Read(sp, pdata);
					ptr = Marshal.PtrToStructure<uint>(phandleAddr);
					Cpu[HvReg.RSP] = sp + 4;
					PageManager.Instance.Read(ptr, data);
				} else {
					ptr = Cpu[reg.Value];
					PageManager.Instance.Read(ptr, data);
				}
				return caller(handleAddr);
			}, value => {
				Marshal.StructureToPtr(value, handleAddr, true);
				PageManager.Instance.Write(ptr, data);
			});
		}

		Action MakeWrapper(MethodInfo method, CallingConvention cc) {
			var parameters = method.GetParameters();
			var numParams = parameters.Length;
			var calleeParams = new object[numParams];
			var pre = new Func<object>[numParams];
			var post = new Action<object>[numParams];
			var fcp = new[] { (HvReg?) HvReg.RCX, HvReg.RDX };
			
			parameters.ForEach((param, i) => {
				if(param.IsIn)
					(pre[i], post[i]) = MakeInWrapper(param, cc == CallingConvention.Fastcall && i < 2 ? fcp[i] : null);
				else if(param.IsOut)
					(pre[i], post[i]) = MakeOutWrapper(param, cc == CallingConvention.Fastcall && i < 2 ? fcp[i] : null);
				else if(param.ParameterType.IsByRef)
					(pre[i], post[i]) = MakeRefWrapper(param, cc == CallingConvention.Fastcall && i < 2 ? fcp[i] : null);
				else
					(pre[i], post[i]) = MakePlainWrapper(param, cc == CallingConvention.Fastcall && i < 2 ? fcp[i] : null);
			});
			
			if(method.ReturnType == typeof(void))
				return () => {
					for(var i = 0; i < numParams; ++i)
						calleeParams[i] = pre[i]?.Invoke();
					method.Invoke(this, calleeParams);
					for(var i = 0; i < numParams; ++i)
						post[i]?.Invoke(calleeParams[i]);
				};
			else {
				var retType = method.ReturnType;
				var size = Extensions.SizeOf(retType);
				Debug.Assert(size <= 4);
				var ptr = Marshal.AllocHGlobal(4);
				var arr = new byte[4];
				return () => {
					for(var i = 0; i < numParams; ++i)
						calleeParams[i] = pre[i]?.Invoke();
					var ret = method.Invoke(this, calleeParams);
					for(var i = 0; i < numParams; ++i)
						post[i]?.Invoke(calleeParams[i]);
					Marshal.StructureToPtr(ret.GetType().IsEnum ? Convert.ChangeType(ret, Enum.GetUnderlyingType(ret.GetType())) : ret, ptr, true);
					Marshal.Copy(ptr, arr, 0, size);
					Cpu[HvReg.RAX] = BitConverter.ToUInt32(arr, 0);
				};
			}
		}
	}
}