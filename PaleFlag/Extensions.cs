using System;
using System.Runtime.InteropServices;

namespace PaleFlag {
	public static class Extensions {
		public static T ToStruct<T>(this byte[] arr, int offset = 0) {
			var gch = GCHandle.Alloc(arr, GCHandleType.Pinned);
			var data = Marshal.PtrToStructure<T>(offset == 0 ? gch.AddrOfPinnedObject() : Marshal.UnsafeAddrOfPinnedArrayElement(arr, offset));
			gch.Free();
			return data;
		}
	}
}