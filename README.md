# VRChat Auto Toggle Creator
A Unity Editor tool to automatically setup the lengthy process of making animations, setitng up a controller, and filling out the vrchat expression assets.
While this is made with VRChat in mind, that is only the tail end of the script and can be used for a variety of tasks related to generating animations and configuring animators.<p align="right">[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/N4N06S00V)</p>

## [DOWNLOAD](https://github.com/CascadianWorks/VRC-Auto-Toggle-Creator/releases)

# How to Use
1. Download the unitypackage from Releases using the download link above.
2. Import the unitypackage and navigate to the top bar and select Cascadian/AutoToggleCreator
4. Make sure your avatar has the VRC_Descriptor and has the FXAniamtionController, VRCExpressionsMenu and VRCExpressionParameters assets attached then click the auto fill button at the top. (Or drag in the four fields manually).
5. Next, click the plus button to add the number of toggles you would like. Same goes for the amount of object and shapekey toggles.
6. You can check the invert box if you do not want the animation to switch to the oppotiote state when activated.
7. When that is done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
8. Upload to VRChat and you should have a seperate toggle for each group you added!

# Video Example of Use
https://user-images.githubusercontent.com/90723146/139313836-1ec916b7-0690-41e6-8618-0a07ccd5f799.mp4

# How It Works
What this editor tool does is generate an animation clip and keyframes inside of it for the corresponging toggle object (also checks to see if the default should be activating or deactivating the object). Once the clips are generated and places in the assets, the animator controller is accessed and a new layer and parameter is made for each toggle object. The transitions are setup in the configuration VRChat needs to behave with their expressions system. Once that is taken care of all the smaller settings/values set, the VRCExpressionsParameters and VRCExpressionsMenu assets are accessed and filled using the same naming conventions as with the animator controller. A control is made and assigned with the parameter and it's done!
