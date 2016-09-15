namespace DtronixMessageQueue {
	public interface IProcessSession {

		/// <summary>
		/// Internal method called by the Postmaster on a different thread to process all bytes in the inbox.
		/// </summary>
		/// <returns>True if incoming queue was processed; False if nothing was available for process.</returns>
		bool ProcessIncomingQueue();

		/// <summary>
		/// Internally called method by the Postmaster on a different thread to send all messages in the outbox.
		/// </summary>
		/// <returns>True if messages were sent.  False if nothing was sent.</returns>
		bool ProcessOutbox();
	}
}