using System;

namespace UniBridge
{
    [Flags]
    public enum StoreTargetMask
    {
        None       = 0,
        GooglePlay = 1 << 0,
        RuStore    = 1 << 1,
        AppStore   = 1 << 2,
        Playgama   = 1 << 3,
        Editor     = 1 << 4,
    }
}
