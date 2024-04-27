namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    // https://api.frankerfacez.com/docs/#/
    public static class FFZEmoteModifierFlags
    {
        // The modifier should not be rendered. This is set for modifiers that have other visual effects on targets. They should not be displayed, and only have images for use when used incorrectly (and thus appearing as a normal emote) or for display in UX such as emote menus.
        public const int HIDDEN = 1 << 0;

        // The target emote should be flipped horizontally.
        public const int FLIP_X = 1 << 1;

        // The target emote should be flipped vertically.
        public const int FLIP_Y = 1 << 2;

        // The target emote should have its width doubled.
        public const int GROW_X = 1 << 3;

        // The target emote should have its hue rotated over time, creating a rainbow animation effect.
        public const int RAINBOW = 1 << 11;

        // The target emote should have its image modified to turn it red and increase its contrast to create an effect similar to "HYPER" emotes.
        public const int HYPER_RED = 1 << 12;

        // The target emote should shake quickly to create an effect similar to "HYPER" emotes.
        public const int HYPER_SHAKE = 1 << 13;

        // The target emote should have its image modified to greatly increase its contrast while reducing its brightness slightly and reducing it to greyscale, creating an effect similar to cursed / despair emotes.
        public const int CURSED = 1 << 14;

        // The target emote should have an animation applied to create the illusion of headbanging / shaking to a rhythm.
        public const int JAM = 1 << 15;

        // The target emote should have an animation applied so that it squishes down, flips horizontally, squishes down again, and flips back to the original orientation before repeating.
        public const int BOUNCE = 1 << 16;
    }
}
