using System.Collections.Generic;

namespace PaleFlag {
	public interface IHandle {
		uint Handle { get; set; }
		void Close();
	}
	
	public class HandleManager {
		uint HandleIter;
		readonly Dictionary<uint, IHandle> Handles = new Dictionary<uint,IHandle>();

		public T Get<T>(uint handle) where T : IHandle => (T) Handles[handle]; 

		public uint Add(IHandle obj) {
			lock(this) {
				Handles[++HandleIter] = obj;
				obj.Handle = HandleIter;
				return HandleIter;
			}
		}

		public T Register<T>(T obj) where T : IHandle {
			Add(obj);
			return obj;
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