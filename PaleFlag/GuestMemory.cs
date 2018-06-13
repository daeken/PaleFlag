using System.Runtime.InteropServices;

namespace PaleFlag {
	public struct GuestMemory<T> where T : struct {
		public uint GuestAddr;

		public GuestMemory(uint addr) => GuestAddr = addr;

		public T Value {
			get => PageManager.Instance.Read<T>(GuestAddr);
			set => PageManager.Instance.Write(GuestAddr, value);
		}
		
		public T this[int index] {
			get => PageManager.Instance.Read<T>((uint) (GuestAddr + Marshal.SizeOf<T>() * index));
			set => PageManager.Instance.Write((uint) (GuestAddr + Marshal.SizeOf<T>() * index), value);
		}
		
		public static implicit operator GuestMemory<T>(uint addr) => new GuestMemory<T>(addr);
		public static implicit operator uint(GuestMemory<T> gm) => gm.GuestAddr;

		public static implicit operator bool(GuestMemory<T> gm) => gm.GuestAddr != 0;
	}

	public class GuestMemoryWrapper<T> where T : struct {
		GuestMemory<T> Guest;

		public GuestMemoryWrapper(uint addr) => Guest = new GuestMemory<T>(addr);

		public T Value {
			get => Guest.Value;
			set => Guest.Value = value;
		}
	}
}