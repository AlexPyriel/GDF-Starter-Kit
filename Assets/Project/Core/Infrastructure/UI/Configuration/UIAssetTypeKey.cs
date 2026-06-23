using System;

namespace Assets.Project.Core.Infrastructure.UI.Configuration
{
    /// <summary>
    /// Builds stable runtime keys for UI component types stored inside UI prefab repositories.
    /// </summary>
    public static class UIAssetTypeKey
    {
        /// <summary>
        /// Gets the repository key for the provided UI component type.
        /// </summary>
        /// <param name="type">Concrete UI component type used as the repository identity.</param>
        /// <returns>Stable runtime key based on the component full name.</returns>
        public static string Get(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.FullName ?? throw new InvalidOperationException($"Type '{type}' does not have a valid full name.");
        }

        /// <summary>
        /// Gets the repository key for the provided UI component type parameter.
        /// </summary>
        public static string Get<T>()
        {
            return Get(typeof(T));
        }
    }
}
