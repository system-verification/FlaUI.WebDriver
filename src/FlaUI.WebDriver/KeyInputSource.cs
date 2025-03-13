using System.Collections.Generic;

namespace FlaUI.WebDriver;

/// <summary>
/// A key input source is an input source that is associated with a keyboard-type device.
/// </summary>
/// <see cref="https://www.w3.org/TR/webdriver2/#key-input-source"/>
public class KeyInputSource() : InputSource("key")
{
    public HashSet<string> Pressed { get; } = new HashSet<string>();

    public bool Alt { get; set; }
    public bool Ctrl{ get; set; }
    public bool Meta { get; set; }
    public bool Shift { get; set; }

    // New backing collection for Unicode keys that have been handled.
    private readonly HashSet<string> _unicodeKeysHandled = new HashSet<string>();

    public void MarkUnicodeKeyHandled(string key)
    {
        _unicodeKeysHandled.Add(key);
    }

    public bool IsUnicodeKeyHandled(string key)
    {
        return _unicodeKeysHandled.Contains(key);
    }

    public void UnmarkUnicodeKeyHandled(string key)
    {
        _unicodeKeysHandled.Remove(key);
    }

    public void Reset()
    {
        Pressed.Clear();
        Alt = false;
        Ctrl = false;
        Meta = false;
        Shift = false;
    }
}