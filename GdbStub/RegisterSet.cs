using System;
using System.Collections.Generic;
using System.Linq;

namespace GdbStub {
	public abstract class RegisterSet {
		public abstract List<(Type Type, string Name)> Registers { get; }
		public abstract string PcRegister { get; }
		public abstract string SpRegister { get; }

		public readonly Dictionary<string, int> NameToNumber;
		public readonly Dictionary<int, string> NumberToName;

		public RegisterSet() {
			NameToNumber = Registers.Select((x, i) => (x, i)).ToDictionary(x => x.Item1.Name, x => x.Item2);
			NumberToName = Registers.Select((x, i) => (x, i)).ToDictionary(x => x.Item2, x => x.Item1.Name);
		}
		
		public int RegisterNumber(string name) =>
			Registers.Select((x, i) => (x, i)).First(x => x.Item1.Name == name).Item2;
	}

	public class X86RegisterSet : RegisterSet {
		public static readonly X86RegisterSet Instance = new X86RegisterSet();

		public override List<(Type Type, string Name)> Registers { get; } = new List<(Type Type, string Name)> {
			(typeof(uint), "EAX"), 
			(typeof(uint), "ECX"), 
			(typeof(uint), "EDX"), 
			(typeof(uint), "EBX"), 
			(typeof(uint), "ESP"), 
			(typeof(uint), "EBP"), 
			(typeof(uint), "ESI"), 
			(typeof(uint), "EDI"), 
			(typeof(uint), "EIP"), 
			(typeof(uint), "EFLAGS"), 
			(typeof(ushort), "CS"), 
			(typeof(ushort), "DS"), 
			(typeof(ushort), "ES"), 
			(typeof(ushort), "FS"), 
			(typeof(ushort), "GS"), 
			(typeof(ushort), "SS")
		};

		public override string PcRegister => "EIP";
		public override string SpRegister => "ESP";
	}
}