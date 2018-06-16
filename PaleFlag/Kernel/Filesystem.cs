using System;
using System.Reflection.Metadata;
using static System.Console;
using static PaleFlag.Globals;

namespace PaleFlag.XboxKernel {
	[Flags]
	public enum AccessFlags : uint {
		FileWriteData = 0x00000002
	}

	public enum CreateDisposition : uint {
		Supersede = 0, 
		Open = 1, 
		Create = 2, 
		OpenIf = 3, 
		Overwrite = 4, 
		OverwriteIf = 5
	}

	[Flags]
	public enum FileOptions : uint {
		DirectoryFile = 0x00000001, 
		WriteThrough = 0x00000002, 
		SequentialOnly = 0x00000004, 
		NoImmediateBuffering = 0x00000008, 
		SyncIOAlert = 0x00000010, 
		SyncIONonAlert = 0x00000020, 
		NonDirectoryFile = 0x00000040, 
		CreateTreeConnection = 0x00000080, 
		CompleteIfOpLocked = 0x00000100, 
		NoEaKnowledge = 0x00000200, 
		OpenForRecovery = 0x00000400, 
		RandomAccess = 0x00000800, 
		DeleteOnClose = 0x00001000, 
		OpenByFileId = 0x00002000, 
		OpenForBackupIntent = 0x00004000, 
		NoCompression = 0x00008000, 
		ReserveOpFilter = 0x00010000, 
		OpenReparsePoint = 0x00020000, 
		OpenNoRecall = 0x00040000, 
		OpenForFreeSpaceQuery = 0x00080000
	}
	
	public struct ObjectAttributes {
		public uint RootDirectory;
		public GuestMemory<AnsiString> ObjectName;
		public uint Attributes;
	}

	public struct IoStatusBlock {
		public uint StatusOrPointer;
		public GuestMemory<uint> Information;
	}

	public partial class Kernel {
		[Export(0xBE)]
		NtStatus NtCreateFile(
			out uint fileHandle, 
			AccessFlags accessMask, 
			in ObjectAttributes objectAttributes, 
			out IoStatusBlock ioStatusBlock, 
			GuestMemory<ulong> allocationSize, 
			uint fileAttributes, 
			uint shareAccess, 
			CreateDisposition createDisposition, 
			FileOptions createOptions
		) {
			var fn = objectAttributes.ObjectName.Value.GetString();
			fileHandle = Box.Vfs.Open(fn).Handle;
			ioStatusBlock = new IoStatusBlock();
			return NtStatus.Success;
		}

		[Export(0xC4)]
		NtStatus NtDeviceIoControlFile(
			uint fileHandle, 
			uint eventHandle, 
			uint apcRoutine, 
			uint apcContext, 
			out IoStatusBlock ioStatusBlock, 
			uint ioControlCode, 
			GuestMemory<byte> inputBuffer, 
			uint inputBufferLength, 
			GuestMemory<byte> outputBuffer, 
			uint outputBufferLength
		) {
			var device = Box.HandleManager.Get<IFileHandle>(fileHandle);

			var ibuf = new byte[inputBuffer.GuestAddr == 0 ? 0 : inputBufferLength];
			var obuf = new byte[outputBuffer.GuestAddr == 0 ? 0 : outputBufferLength];

			for(var i = 0; i < ibuf.Length; ++i)
				ibuf[i] = inputBuffer[i];
			
			WriteLine($"Ioctl {ioControlCode:X08} to {device.Name}");
			HexDump(ibuf);
			
			var ret = device.Ioctl(ioControlCode, ibuf, obuf);
			
			WriteLine($"Ioctl ({ret}) out:");
			HexDump(obuf);

			for(var i = 0; i < obuf.Length; ++i)
				outputBuffer[i] = obuf[i];
			
			ioStatusBlock = new IoStatusBlock();

			return ret;
		}

		[Export(0x5B)]
		NtStatus IoDismountVolumeByName(in AnsiString name) {
			WriteLine($"Attempting to dismount '{name.GetString()}'");
			return NtStatus.Success;
		}

		[Export(0x43)]
		NtStatus IoCreateSymbolicLink(in AnsiString symbolicLinkName, in AnsiString deviceName) {
			WriteLine($"Making symbolic link from '{symbolicLinkName.GetString()}' -> '{deviceName.GetString()}'");
			return NtStatus.Success;
		}
	}
}