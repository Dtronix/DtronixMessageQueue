using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQMessage : IList<MQFrame> {

		private readonly List<MQFrame> frames = new List<MQFrame>();

		public MQMessage() {

		}

		public byte[] ToByteArray() {
			var total_size = frames.Sum(frame => frame.FrameLength);
			var buffer = new byte[total_size];
			using (var stream = new MemoryStream(buffer)) {

				for (var i = 0; i < frames.Count; i++) {
					var frame = frames[i];

					// Set the last frame in this message as the last.
					if (i == frames.Count - 1) {
						frame.SetLast();
					}
					var bytes = frame.RawFrame();
					stream.Write(bytes, 0, bytes.Length);
				}

				return buffer;
			}
		}


		public IEnumerator<MQFrame> GetEnumerator() {
			return new List<MQFrame>(frames).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new List<MQFrame>(frames).GetEnumerator();
		}

		public void Add(MQFrame item) {
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
