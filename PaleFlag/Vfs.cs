using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PaleFlag.Devices;
using PaleFlag.XboxKernel;
using static System.Console;

namespace PaleFlag {
	public interface IFileHandle : IHandle {
		string Name { get; }
		NtStatus Ioctl(uint code, byte[] ibuf, byte[] obuf);
		int Read(byte[] buf, int count);
		void Write(byte[] buf, int count);
	}

	public interface IDirHandle : IHandle {
		VfsNode Dir { get; }
	}

	public class DirHandle : IDirHandle {
		public uint Handle { get; set; }
		public void Close() {}

		public VfsNode Dir { get; }
		public DirHandle(VfsNode dir) => Dir = dir;
	}

	public abstract class VfsNode {
		public abstract bool IsFile { get; }
		public readonly string FullPath;

		protected VfsNode(string fullPath) => FullPath = fullPath;

		public virtual Dictionary<string, VfsNode> Children => null;
		
		public virtual IFileHandle OpenFile() => throw new NotImplementedException();
		public virtual IDirHandle OpenDir() => throw new NotImplementedException();
	}

	public class VfsVirtualDirectory : VfsNode {
		public override bool IsFile => false;
		readonly Dictionary<string, VfsNode> _Children = new Dictionary<string, VfsNode>(StringComparer.InvariantCultureIgnoreCase);
		public override Dictionary<string, VfsNode> Children => _Children;

		public VfsVirtualDirectory(string fullPath) : base(fullPath) {}
		
		public override IDirHandle OpenDir() => new DirHandle(this);

		public void Add(string name, VfsNode child) => _Children[name] = child;
	}

	public abstract class VfsDeviceNode : VfsNode {
		public override bool IsFile => true;
		protected VfsDeviceNode(string fullPath) : base(fullPath) {}
	}
	
	public class Vfs {
		readonly Xbox Box;
		public readonly VfsVirtualDirectory Root = new VfsVirtualDirectory(@"\??");

		public Vfs(Xbox box) {
			Box = box;
			AddDevice(new CdromDeviceNode(@"\??\CDROM0:"));
			AddVirtualDirectory(@"\??\Device");
			AddVirtualDirectory(@"\??\Device\Harddisk0");
			AddVirtualDirectory(@"\??\Device\Harddisk0\partition1");
			AddVirtualDirectory(@"\??\Device\Harddisk0\partition2");
			AddVirtualDirectory(@"\??\Device\Harddisk0\partition2\xboxdashdata.185ead00");
		}

		public IHandle OpenFile(string path) {
			var node = FindNode(path);
			return Box.HandleManager.Register(node.OpenFile());
		}

		public IHandle OpenDirectory(string path, bool create = false) {
			VfsNode node;
			if(create) {
				var ppath = ParsePath(path);
				var cur = (VfsNode) Root;
				var found = new List<string> { @"\??" };
				foreach(var elem in ppath) {
					var children = cur.Children;
					if(!children.ContainsKey(elem))
						children[elem] = new VfsVirtualDirectory(string.Join('\\', found));
					found.Add(elem);
					cur = children[elem];
				}
				node = cur;
			} else
				node = FindNode(path);

			return Box.HandleManager.Register(node.OpenDir());
		}

		public (VfsVirtualDirectory, string) GetParentAndName(string path) => GetParentAndName(ParsePath(path));

		public (VfsVirtualDirectory, string) GetParentAndName(List<string> path) {
			var ppath = path.Select(x => x).ToList();
			var name = ppath.Last();
			ppath.RemoveAt(ppath.Count - 1);
			var parent = (VfsVirtualDirectory) FindNode(ppath);
			return (parent, name);
		}

		public void AddVirtualDirectory(string path) {
			var (parent, name) = GetParentAndName(path);
			parent.Add(name, new VfsVirtualDirectory(path));
		}

		public void AddDevice(VfsDeviceNode device, string path = null) {
			var (parent, name) = GetParentAndName(path ?? device.FullPath);
			parent.Add(name, device);
		}

		public void CreateSymlink(string from, string to) {
			var tn = FindNode(to);
			var (fparent, fname) = GetParentAndName(from);
			fparent.Add(fname, tn);
		}

		public VfsNode FindNode(string path) => FindNode(ParsePath(path));
		public VfsNode FindNode(List<string> path) {
			var found = new List<string>();
			var cur = (VfsNode) Root;
			foreach(var elem in path) {
				var children = cur.Children;
				if(children == null || !children.ContainsKey(elem))
					throw new FileNotFoundException($"Node [{string.Join('\\', path)}] not found.  Only found [\\??\\{string.Join('\\', found)}]", string.Join('\\', path));
				found.Add(elem);
				cur = children[elem];
			}
			return cur;
		}

		public static List<string> ParsePath(string path) {
			if(path.StartsWith(@"\??\"))
				path = path.Substring(4);
			
			var elements = path.Split('\\').Where(x => x != "." && x != "").ToList();

			while(true) {
				var pos = elements.IndexOf("..");
				Debug.Assert(pos != 0);
				if(pos == -1) break;
				elements.RemoveAt(pos);
				elements.RemoveAt(pos - 1);
			}

			return elements;
		}
	}
}