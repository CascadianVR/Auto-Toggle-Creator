# VRChat Auto Toggle Creator
A unity editor tool to automatically setup the lengthy process of making animations, setitng up a controller, and filling out the vrchat expression assets.

# How to Use
1. Open the menu from from Tools/Cascadian/AutoToggleCreator
2. Make sure your avatar has the VRC_Descriptor and has the FXAniamtionController, VRCExpressionsMenu and VRCExpressionParameters assets attached then click the auto fill button at the top. (Or drag in the four fields manually).
3. Next, drag in your game objects you would like to make a toggle for.
4. You can check the two boxes if you want vrchat to remember the settings in game or if the objects you are toggling are on by default.
5. When that is done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
6. Upload to VRChat and you should have a seperate toggle for each game object you assigned!
# Settings
- ***Save VRC Parameters:*** If this button is checked, it will tell vrchat to remember the previous parameter value in-game when you switch to the avatar.
- ***Start On by Default:*** Check this box if your meshes are enabled (not disabled) by default. IF your object are a mix of both right now you'll have to group them and apply the tool twice - one for off and other for on.
# Current known Issues
- When toggles are created, the VRCExpressionMenu item duplicates if one with the same name already exists.
- Random edge cases where error can occure due to names, order or invalid objects.

# Video Example of Use
https://user-images.githubusercontent.com/90723146/139313836-1ec916b7-0690-41e6-8618-0a07ccd5f799.mp4

