using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    public abstract class SecurityObject : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Gets a value indicating whether this security object has been disposed.
        /// </summary>
        public bool Disposed => _disposed;

        /// <summary>
        /// Gets the unique identifier of this security object.
        /// </summary>
        public abstract string UniqueIdentifier { get; }

        /// <summary>
        /// Gets a string that represents the current state of this security object.
        /// </summary>
        /// <remarks>
        /// The value must be changed for every state change of this object.
        /// It is used to check if the object is still valid.
        /// </remarks>
        public abstract string CurrentState { get; }

        /// <summary>
        /// Gets the signature of this security object.
        /// </summary>
        public ImmutableArray<byte>? Signature { get; internal set; }

        public SecurityContext? Parent { get; internal set; }

        public virtual bool IsValid()
        {
            return !Disposed
                && Parent != null
                && !Parent.Disposed
                && Parent.ValidateSecurityObject(this);
        }

        protected string CreateStateString(Dictionary<string, string> properties)
        {
            var sb = new StringBuilder();
            sb.Append($"{UniqueIdentifier}");
            sb.Append("{");

            foreach (var kvp in properties)
            {
                sb.Append($"{kvp.Key}={kvp.Value},");
            }

            sb.Append("}");
            return sb.ToString();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Parent = null;
                Signature = null;

                _disposed = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public override string ToString() => UniqueIdentifier;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is SecurityObject securityObject
                && CurrentState == securityObject.CurrentState;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return CurrentState.GetHashCode();
        }

        /// <summary>
        /// Compares two <see cref="SecurityObject"/> instances for equality.
        /// </summary>
        /// <param name="left">
        /// The left operand of the equality operator.
        /// </param>
        /// <param name="right">
        /// The right operand of the equality operator.
        /// </param>
        /// <returns>
        /// true if the two <see cref="SecurityObject"/> instances are equal; otherwise, false.
        /// </returns>
        public static bool operator ==(SecurityObject left, SecurityObject right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="SecurityObject"/> instances for inequality.
        /// </summary>
        /// <param name="left">
        /// The left operand of the inequality operator.
        /// </param>
        /// <param name="right">
        /// The right operand of the inequality operator.
        /// </param>
        /// <returns>
        /// true if the two <see cref="SecurityObject"/> instances are not equal; otherwise, false.
        /// </returns>
        public static bool operator !=(SecurityObject left, SecurityObject right)
        {
            return !(left == right);
        }
    }
}
