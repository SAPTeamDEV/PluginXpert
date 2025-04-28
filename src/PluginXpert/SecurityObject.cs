using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPTeam.PluginXpert
{
    /// <summary>
    /// Base class for all security objects.
    /// </summary>
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

        /// <summary>
        /// Gets the parent <see cref="SecurityContext"/> of this security object.
        /// </summary>
        public SecurityContext? Parent { get; internal set; }

        /// <summary>
        /// Checks if this security object is valid in the given security context.
        /// </summary>
        /// <param name="securityContext">
        /// The security context to check against.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this security object is valid in the given security context; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsValid(SecurityContext securityContext)
        {
            return Parent == securityContext
                   && IsValid();
        }

        /// <summary>
        /// Checks if this security object is valid.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this security object is valid; otherwise, <see langword="false"/>."
        /// </returns>
        public virtual bool IsValid()
        {
            return !Disposed
                && Parent != null
                && !Parent.Disposed
                && Parent.VerifySecurityObject(this);
        }

        /// <summary>
        /// Creates a normal current state string from the unique identifier and the properties.
        /// </summary>
        /// <param name="properties">
        /// The properties to include in the state string.
        /// </param>
        /// <returns>
        /// A string that represents the current state of this security object.
        /// </returns>
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

        /// <summary>
        /// Disposes the current security object.
        /// </summary>
        /// <param name="disposing">
        /// true if the method is called from Dispose; false if it is called from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _ = Parent?.RemoveSecurityObject(this);
                }

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
        public static bool operator ==(SecurityObject? left, SecurityObject? right)
        {
            return left!.Equals(right);
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
        public static bool operator !=(SecurityObject? left, SecurityObject? right)
        {
            return !(left == right);
        }
    }
}
