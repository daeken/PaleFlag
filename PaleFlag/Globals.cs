using static System.Console;

namespace PaleFlag {
	public static class Globals {
		public static GuestMemoryWrapper<T> guest<T>(uint addr) where T : struct => new GuestMemoryWrapper<T>(addr);

		public static void HexDump(byte[] data) {
			for(var i = 0; i < data.Length; i += 16) {
				var line = $"{i:X04} | ";
				for(var j = 0; j < 16; ++j) {
					if(i + j >= data.Length)
						break;
					line += $"{data[i + j]:X02} ";
					if(j == 7)
						line += " ";
				}
				WriteLine(line);
			}
			WriteLine($"{data.Length:X04}");
		}
	}
}