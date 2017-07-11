using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Message which contains one or more frames to read or write to a MqClient or MqServer
    /// </summary>
    public class MqMessage : IList<MqFrame>
    {
        /// <summary>
        /// Internal frames for this message.
        /// </summary>
        private readonly List<MqFrame> _frames = new List<MqFrame>();


        /// <summary>
        /// Total number of frames in this message.
        /// </summary>
        public int Count => _frames.Count;

        /// <summary>
        /// Whether or not this is a read only message.  Always returns false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the frame at the specified index.
        /// </summary>
        /// <returns>The frame at the specified index.</returns>
        /// <param name="index">The zero-based index of the frame to get or set.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the message.</exception>
        public MqFrame this[int index]
        {
            get { return _frames[index]; }
            set { _frames[index] = value; }
        }


        /// <summary>
        /// The total size of the raw frames (headers + body) contained in this message.
        /// </summary>
        public int Size => _frames.Sum(frame => frame.FrameSize);

        /// <summary>
        /// The total size of the raw frames minus the headers contained in this message.
        /// </summary>
        public int DataLength => _frames.Sum(frame => frame.DataLength);

        public MqMessage()
        {
        }

        public MqMessage(MqFrame frame)
        {
            Add(frame);
        }

        /// <summary>
        /// Fixes any mistakes for the frames' FrameType set.  Called before frames are processed by the outbox.
        /// </summary>
        public void PrepareSend()
        {
            var mqFrames = _frames.ToArray();

            // Set frame's FrameType appropriately.
            foreach (var frame in mqFrames)
            {
                if (frame.FrameType != MqFrameType.Command &&
                    frame.FrameType != MqFrameType.Ping)
                {
                    frame.FrameType = frame.DataLength == 0 ? MqFrameType.Empty : MqFrameType.More;
                }
            }

            // Set the last frame to the "last frame" FrameType.
            mqFrames[mqFrames.Length - 1].SetLast();
        }


        /// <summary>
        /// Returns an enumerator that iterates through the frames.
        /// </summary>
        /// <returns>An enumerator for the frames</returns>
        public IEnumerator<MqFrame> GetEnumerator()
        {
            return new List<MqFrame>(_frames).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the frames.
        /// </summary>
        /// <returns>An enumerator for the frames</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new List<MqFrame>(_frames).GetEnumerator();
        }

        /// <summary>
        /// Adds a frame to this message.
        /// </summary>
        /// <param name="frame">Frame to add</param>
        public void Add(MqFrame frame)
        {
            _frames.Add(frame);
        }

        /// <summary>
        /// Adds an existing message's frames to this message.
        /// </summary>
        /// <param name="message">Message to add.</param>
        public void Add(MqMessage message)
        {
            foreach (var frame in message)
            {
                _frames.Add(frame);
            }
        }

        /// <summary>
        /// Adds a frame to this message.
        /// </summary>
        /// <param name="frames">Frames to add</param>
        public void AddRange(IEnumerable<MqFrame> frames)
        {
            foreach (var frame in frames)
            {
                _frames.Add(frame);
            }
        }

        /// <summary>
        /// Removes all frames from the message.
        /// </summary>
        public void Clear()
        {
            _frames.Clear();
        }

        /// <summary>
        /// Determines whether a frame is in this message.
        /// </summary>
        /// <param name="frame">The frame to locate in the message.</param>
        /// <returns>True if the frame is in the message; otherwise false.</returns>
        public bool Contains(MqFrame frame)
        {
            return _frames.Contains(frame);
        }

        /// <summary>
        /// Copies all message frames to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.List`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">The number of elements in the source message is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
        public void CopyTo(MqFrame[] array, int arrayIndex)
        {
            _frames.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removed the first occurrence of the specified frame 
        /// </summary>
        /// <param name="frame">Frame to remove</param>
        /// <returns></returns>
        public bool Remove(MqFrame frame)
        {
            return _frames.Remove(frame);
        }

        /// <summary>
        /// Searches for the specified frame and returns the zero-based index of the first occurrence within the message.
        /// </summary>
        /// <returns>The zero-based index of the first occurrence of frame within the message, if found; otherwise, –1.</returns>
        /// <param name="frame">The object to locate in the message.</param>
        public int IndexOf(MqFrame frame)
        {
            return _frames.IndexOf(frame);
        }


        /// <summary>
        /// Inserts an element into the message at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the frame should be inserted.</param>
        /// <param name="frame">The object to insert. The value can be null for reference types.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is less than 0.-or-<paramref name="index" /> is greater than the total frames.</exception>
        public void Insert(int index, MqFrame frame)
        {
            _frames.Insert(index, frame);
        }

        /// <summary>
        /// Removes the frame at the specified index of the message.
        /// </summary>
        /// <param name="index">The zero-based index of the frame to remove.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> is less than 0.-or-<paramref name="index" /> is equal to or greater than the total frames.</exception>
        public void RemoveAt(int index)
        {
            _frames.RemoveAt(index);
        }

        /// <summary>
        /// Shallow copies this message with shallow copied frames.
        /// </summary>
        /// <returns>Shallow copied message.</returns>
        public MqMessage ShallowCopy()
        {
            var message = new MqMessage();
            foreach (var frame in _frames)
            {
                message.Add(frame.ShallowCopy());
            }

            return message;
        }


        /// <summary>
        /// Deep copies this message with deep copied frames.
        /// </summary>
        /// <returns>Deep copied message.</returns>
        public MqMessage Clone()
        {
            var message = new MqMessage();
            foreach (var frame in _frames)
            {
                message.Add(frame.Clone());
            }

            return message;
        }

        /// <summary>
        /// Displays total frames and payload size.
        /// </summary>
        /// <returns>string representation of this message.</returns>
        public override string ToString()
        {
            var size = _frames.Sum(frame => frame.DataLength);
            return $"MqMessage with {_frames.Count} frames totaling {size:N0} bytes.";
        }
    }
}