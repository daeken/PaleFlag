using System.Runtime.InteropServices;

namespace PaleFlag {
	public struct GuestMemory<T> where T : struct {
		public readonly uint GuestAddr;

		public GuestMemory(uint addr) => GuestAddr = addr;

		public T Value {
			get => PageManager.Instance.Read<T>(GuestAddr);
			set => PageManager.Instance.Write(GuestAddr, value);
		}
		
		public T this[int index] {
			get => PageManager.Instance.Read<T>((uint) (GuestAddr + Marshal.SizeOf<T>() * index));
			set => PageManager.Instance.Write((uint) (GuestAddr + Marshal.SizeOf<T>() * index), value);
		}
	}
}