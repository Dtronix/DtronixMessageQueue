namespace DtronixMessageQueue.Socket {

	/// <summary>
	/// Class to implement on classes which have setup events.
	/// </summary>
	/// <typeparam name="TConfig"></typeparam>
	public interface ISetupSocketSession {

		/// <summary>
		/// Start the session's receive events.
		/// </summary>
		void Start();
	}
}