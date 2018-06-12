namespace PaleFlag {
	public class Thread {
		public readonly uint Id;
		public uint Eip, Eflags, Eax, Ecx, Edx, Ebx, Esi, Edi, Esp, Ebp;
		public ushort Cs, Ss, Ds, Es, Fs, Gs;
	}
	
	public class ThreadManager {
		readonly Xbox Box;

		public ThreadManager(Xbox box) => Box = box;
	}
}