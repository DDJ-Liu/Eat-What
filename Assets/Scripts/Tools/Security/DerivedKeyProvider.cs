using System;
namespace EatWhat.Tools.Security
{
    /// <summary>Independent labelled subkey from the same stable master; no second configured secret.</summary>
    public sealed class DerivedKeyProvider : IKeyProvider
    {
        private readonly IKeyProvider master;
        private readonly string purpose;
        public DerivedKeyProvider(IKeyProvider master, string purpose)
        { if (master == null || string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException("Master provider and purpose are required."); this.master = master; this.purpose = purpose; }
        public byte[] GetKey() { return KeyDerivation.Derive(master, purpose)[0]; }
    }
}
