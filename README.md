# VRChat Auto Toggle Setup
A untiy editor tool to automatically setup the lengthy process of making animations, setitng up a controller, and filling out the vrchat expression assets.

# How to Use
1. Open the menu from from Tools/Cascadian/AutoToggleCreator
2. Make sure your avatar has the VRC_Descriptor and has the FXAniamtionController, VRCExpressionsMenu and VRCExpressionParameters assets attached then click the auto fill button at the top. (Or drag in the four fields manually).
3. Next, drag in your game objects you would like to make a toggle for.
4. You can check the two boxes if you want vrchat to remember the settings in game or if the objects you are toggling are on by default.
5. When that is done, you can click the "Create Toggles!" button and it will create the animation, layers, parameters and expression items needed.
6. Upload to VRChat and you should have a seperate toggle for each game object you assigned!
# Current known issues
- When toggles are created, the VRCExpressionMenu item duplicates if one with the same name already exists.
- 