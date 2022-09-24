﻿using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Screenbox.Core.Messages
{
    internal sealed class ChangeVolumeMessage : ValueChangedMessage<int>
    {
        public bool IsOffset { get; }

        public ChangeVolumeMessage(int value, bool offset = false) : base(value)
        {
            IsOffset = offset;
        }
    }
}
