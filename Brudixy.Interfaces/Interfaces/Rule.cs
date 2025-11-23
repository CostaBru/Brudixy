namespace Brudixy.Interfaces
{
    public enum Rule
    {
        /// <summary>No action taken on related rows.</summary>
        /// <returns></returns>
        None,
        /// <summary>Delete or update related rows. This is the default.</summary>
        /// <returns></returns>
        Cascade,
        /// <summary>Set values in related rows to null.</summary>
        /// <returns></returns>
        SetNull,
        /// <summary>Set values in related rows to the DataColumn's default value.</summary>
        /// <returns></returns>
        SetDefault,
    }
}