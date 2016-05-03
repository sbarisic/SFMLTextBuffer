# SFMLTextBuffer
TextBuffers for SFML

# Screenshots
![alt text](https://raw.githubusercontent.com/cartman300/SFMLTextBuffer/master/screenshots/a.png "Hello World!")

![alt text](https://raw.githubusercontent.com/cartman300/SFMLTextBuffer/master/screenshots/b.png "Colors")

![alt text](https://raw.githubusercontent.com/cartman300/SFMLTextBuffer/master/screenshots/c.png "More colors")

# Example usage
```c#
TextBuffer TBuffer = new TextBuffer(80, 25, new Texture("font.png"));
TBuffer.Print(10, 10, "Hello World!");
// TBuffer.Sprite.* - Change position, color, whatever.

RenderTarget.Draw(TBuffer);
```

# C++ version by harddal
https://github.com/harddal/TextBuffer

# License
Do whatever you want etc. etc.
