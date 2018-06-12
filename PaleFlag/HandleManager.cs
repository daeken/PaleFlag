using System.Collections.Generic;

namespace PaleFlag {
	public interface IHandle {
		void Close();
	}
	
	public class HandleManager {
		public uint HandleIter;
		public readonly Dictionary<uint, IHandle> Handles = new Dictionary<uint,IHandle>();

		public uint Add(IHandle obj) {
			lock(this) {
				Handles[++HandleIter] = obj;
				return HandleIter;
			}
		}

		public void Close(uint handle) {
			lock(this) {
				if(!Handles.ContainsKey(handle)) return;
				Handles[handle].Close();
				Handles.Remove(handle);
			}
		}
	}
}