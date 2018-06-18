using System;

namespace PaleFlag {
	class Program {
		static void Main(string[] args) => new Xbox(args[0], args.Length > 1 && args[1] == "--gdb").Start();
	}
}