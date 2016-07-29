using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQMessage :IEnumerable<MQFrame>, IList<MQFrame> {

		private bool complete;
		private readonly List<MQFrame> frames = new List<MQFrame>();

		/// <summary>
		/// True if this message is complete.
		/// </summary>
		public bool Complete => complete;

		public MQMessage() {

		}



		public int Write(byte[] bytes, int offset, int count) {
			MQFrame frame = null;
			if (frames.Count != 0) {
				frame = frames[frames.Count - 1];
			}

			if (frame == null || frame.FrameComplete) {
				frame = new MQFrame();
				frames.Add(frame);
			}

			int over_read = count;
			while ((over_read = frame.Write(bytes, offset + count - over_read, over_read)) > 0) {
				if (frame.FrameComplete && frame.FrameType == MQFrameType.More) {
					// We have a complete frame and there is another frame following this frame.
					frame = new MQFrame();
					frames.Add(frame);
					continue;
				}

				if (frame.FrameComplete && frame.FrameType == MQFrameType.Last) {
					// Done reading frames for this message.
					break;
				}

				// If we still over read at this point, send it back to the original callee.
				if (over_read > 0) {
					break;
				}
			}

			if (frame.FrameComplete && frame.FrameType == MQFrameType.Last) {
				complete = true;
			}

			return over_read;

		}

		public byte[] ToByteArray() {
			int total_size = frames.Sum(frame => frame.FrameLength);
			using (MemoryStream stream = new MemoryStream(total_size)) {

				for (var i = 0; i < frames.Count; i++) {
					var frame = frames[i];

					// Set the last frame in this message as the last.
					if (i == frames.Count - 1) {
						frame.FrameType = MQFrameType.Last;
					}
					var bytes = frame.RawFrame();
					stream.Write(bytes, 0, bytes.Length);
				}


				return stream.GetBuffer();
			}
		}


		public IEnumerator<MQFrame> GetEnumerator() {
			return new List<MQFrame>(frames).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new List<MQFrame>(frames).GetEnumerator();
		}

		public void Add(MQFrame item) {
			if (frames.Count > 0) {
				frames[frames.Count -1].FrameType = MQFrameType.More;
			}
			frames.Add(item);
		}

		public void Clear() {
			frames.Clear();
		}

		public bool Contains(MQFrame item) {
			return frames.Contains(item);
		}

		public void CopyTo(MQFrame[] array, int array_index) {
			frames.CopyTo(array, array_index);
		}

		public bool Remove(MQFrame item) {
			return frames.Remove(item);
		}

		public int Count => frames.Count;
		public bool IsReadOnly => false;
		public int IndexOf(MQFrame item) {
			return frames.IndexOf(item);
		}

		public void Insert(int index, MQFrame item) {
			frames.Insert(index, item);
		}

		public void RemoveAt(int index) {
			frames.RemoveAt(index);
		}

		public MQFrame this[int index] {
			get { return frames[index]; }
			set { frames[index] = value; }
		}
	}
}
