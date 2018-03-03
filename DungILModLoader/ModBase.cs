namespace DungILModLoader
{
    public abstract class ModBase
    {
        public ModManifest Manifest { get; set; }

        public virtual void Init() { }
    }
}
