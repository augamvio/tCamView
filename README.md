## tCamView

tCamView is a simple WebCam viewer software designed for educational use.
You can display your webcam video on top of the computer screen with the following features.

![feature1](https://github.com/augamvio/tCamView/raw/main/image1.jpg)  
![feature2](https://github.com/augamvio/tCamView/raw/main/image2.jpg)  
![feature3](https://github.com/augamvio/tCamView/raw/main/image3.jpg)  
![feature4](https://github.com/augamvio/tCamView/raw/main/image4.jpg)  

## Features

- 4 PictureSize Modes
  * Zoom [Z]: Enlarge to fit the window size while maintaining the aspect ratio of the webcam video (aka. Uniform)
  * Stretch [X]: Enlarge to fill the window size without considering the aspect ratio of the webcam video (aka. Fill)
  * Center [C]: No resizing. If the window size is changed, the center area of ​​the webcam video is displayed
  * Alt.Stretch [A]: Enlarge to fill the window size while maintaining the aspect ratio of the webcam video. If the window size is changed, the webcam video content is clipped to fit in the window dimensions. (aka. UniformToFill)
- 5 Window Styles
  * Normal Border (Resizable) [N or Esc]
  * Borderless Ellipse (FixedSize) [E]
  * Borderless Rectangle (FixedSize) [R]
  * Borderless Rounded Rectangle (FixedSize) [W]
  * Full Screen [F]
- Image Flipping
  * Horizontal Flipping [H]
  * Vertical Flipping [V]
- Opacity Control
  * Increase the opacity [Up Arrow]
  * Decrease the opacity [Down Arrow]
  * Opacity 100% (max) [Right Arrow]
  * Opacity 20% (min) [Left Arrow]
- Additional Features regarding the Clipboard
  * GetImage From Clipboard [G or Ctrl+V]
  * SetImage To Clipboard [I or Ctrl+V]
  * SetImage To Clipboard After 5 Seconds [D]
  * CopyScreen To Clipboard [S]
  * Resume CameraPreview (From ClipboardView) [Space]
- Additional Features related to Zoom In/Out
  * CropImage (ZoomIn) [Page Up]
  * CropImage (ZoomOut) [Page Down]
- Additional Features related to Window Size
  * Increase Window Size [P]
  * Decrease Window Size [M]
- Always on Top [T]
- Minimize [L]
- Quit [Q]
- Display multiple webcam videos with multiple instances
- Move the window with the left mouse button drag
- Borderless Ellipse/Rectangle/Rounded Rectangle Windows can be resized with [P] and [M].

## How to use
Right-click to display the menu 
![menu](https://github.com/augamvio/tCamView/raw/main/image5.jpg)  

or press the Hotkey (see ![ShortCut.txt](https://github.com/augamvio/tCamView/raw/main/ShortCut.txt) ).

## License

Copyright © 2020-2021, Sung Deuk Kim  
All rights reserved.  
Published under the GNU GPLv3 license. (For details, see license.txt)

## Credits

- AForge.NET  http://www.aforgenet.com/  (license: lgpl-3.0.txt, gpl-3.0.txt)
