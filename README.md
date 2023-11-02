# Auto Toggle Creator
A Unity Editor tool to automatically setup the lengthy process of making animations, setitng up a controller, and filling out the vrchat expression assets.
While this is made with VRChat in mind, that is only the tail end of the script and can be used for a variety of tasks related to generating animations and configuring animators.

# Download
https://github.com/CascadianWorks/VRC-Auto-Toggle-Creator/releases

# How to Use
1. Download the Unitypackage from the releases page (link above) from the download page linked above and import it to your Unity project.
2. Open the menu from the top menu bar under Cascadian/AutoToggleCreator
6. You can click the plus icon to add a toggle and assign any values/object you'd like to. You can add as many toggles as you'd like.
7. When done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
8. Upload to VRChat and you should have a seperate toggle for each game object you assigned!

# Examples
## https://youtu.be/YLPkL_B8m9E

<p float="left">
<img src="https://user-images.githubusercontent.com/90723146/220522896-78576c33-714b-4d03-af5d-6f57d57d47d8.png" width="350">
<img src="https://user-images.githubusercontent.com/90723146/220524423-add04f3e-1f99-4955-98d7-107cc2cfabcc.png" width="600">
</p>
  
# How It Works
What this editor tool does is generate an animation clip and keyframes inside of it for the corresponging toggle object name (also checks to see if the default should be activating or deactivating the object). Once the clips are generated and places in the assets, the animator controller is accessed and a new layer and parameter is made for each toggle object. The transitions are setup in the configuration VRChat needs to behave with their expressions system. Once that is taken care of all the smaller settings/values set, the VRCExpressionsParameters and VRCExpressionsMenu assets are accessed and filled using the same naming conventions as with the animator controller. A control is made and assigned with the parameter and it's done!
