# Auto Toggle Creator
A Unity Editor tool to automatically setup the lengthy process of making animations, setitng up a controller, and filling out the vrchat expression assets.
While this is made with VRChat in mind, that is only the tail end of the script and can be used for a variety of tasks related to generating animations and configuring animators.<p align="right">[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/N4N06S00V)</p>

# Download
https://github.com/CascadianWorks/VRC-Auto-Toggle-Creator/releases

# How to Use
1. Download the Unitypackage from the releases page (link above) from the download page linked above and import it to your Unity project.
2. Open the menu from the top menu bar under Tools/Cascadian/AutoToggleCreator
6. You can click the plus icon to add a toggle and assign any values/object you'd like to. You can add as many toggles as you'd like.
7. When done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
8. Upload to VRChat and you should have a seperate toggle for each game object you assigned!

# Video Example of Use
https://youtu.be/YLPkL_B8m9E

# How It Works
What this editor tool does is generate an animation clip and keyframes inside of it for the corresponging toggle object name (also checks to see if the default should be activating or deactivating the object). Once the clips are generated and places in the assets, the animator controller is accessed and a new layer and parameter is made for each toggle object. The transitions are setup in the configuration VRChat needs to behave with their expressions system. Once that is taken care of all the smaller settings/values set, the VRCExpressionsParameters and VRCExpressionsMenu assets are accessed and filled using the same naming conventions as with the animator controller. A control is made and assigned with the parameter and it's done!
