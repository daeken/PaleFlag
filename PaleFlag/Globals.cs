namespace PaleFlag {
	public static class Globals {
		public static GuestMemoryWrapper<T> guest<T>(uint addr) where T : struct => new GuestMemoryWrapper<T>(addr);
	}
}