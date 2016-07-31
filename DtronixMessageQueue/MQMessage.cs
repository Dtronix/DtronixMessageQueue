using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtronixMessageQueue {
	public class MQMessage : IList<MQFrame> {

		public readonly List<MQFrame> Frames = new List<MQFrame>();

		public MQMessage() {

		}

		/*public byte[] ToByteArray() {
			var total_size = Frames.Sum(frame => frame.FrameLength);
			var buffer = new byte[total_size];
			using (var stream = new MemoryStream(buffer)) {

				for (var i = 0; i < Frames.Count; i++) {
					var frame = Frames[i];

					// Set the last frame in this message as the last.
					if (i == Frames.Count - 1) {
						frame.SetLast();
					}
					var bytes = frame.RawFrame();
					stream.Write(bytes, 0, bytes.Length);
				}

				return buffer;
			}
		}*/


		public IEnumerator<MQFrame> GetEnumerator() {
			return new List<MQFrame>(Frames).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new List<MQFrame>(Frames).GetEnumerator();
		}

		public void Add(MQFrame item) {
			Frames.Add(item);
		}

		public void Clear() {
			Frames.Clear();
		}

		public bool Contains(MQFrame item) {
			return Frames.Contains(item);
		}

		public void CopyTo(MQFrame[] array, int array_index) {
			Frames.CopyTo(array, array_index);
		}

		public bool Remove(MQFrame item) {
			return Frames.Remove(item);
		}

		public int Count => Frames.Count;
		public bool IsReadOnly => false;
		public int IndexOf(MQFrame item) {
			return Frames.IndexOf(item);
		}

		public void Insert(int index, MQFrame item) {
			Frames.Insert(index, item);
		}

		public void RemoveAt(int index) {
			Frames.RemoveAt(index);
		}

		public MQFrame this[int index] {
			get { return Frames[index]; }
			set { Frames[index] = value; }
		}
	}
}
