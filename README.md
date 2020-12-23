# A-Love-Story
Multiplayer VR game using Photon networking for Oculus

A Love Story
Role: Game Programmer
Team Size: 5 (2 programmers, 2 artists, 1 sound designer)
Project Duration: 2 weeks
Input: Oculus Quest/ Rift, Valve Index, Zoom
Plugins: Photon for networking


Programming Challenges:
    -   Photon to create networking component
    -   alleviate latency problem/messages bandwidth in photon networking (especially painful in VR)
    -   configure controls for different headsets​​​​​​​

This is probably the boldest idea we ever come up with to make a multi-player VR game in 2 weeks. In the game you will role play as a girl and meet a boy named Jacob who is played by one of our team member (special thanks to our actor David Morales). You will experience 4 phases: first meet (huge crush), move in together after graduating, have a big fight and finally in the boy's wedding they reconcile and wish each other the best.

The basic implementation logic is: we have 2 scenes: a host scene (controlled by our team member) and a guest scene (controlled by player). Both have their own sets of controls and photon component is responsible for sending data to each side to make sure they are synchronous. 

The biggest challenge is the networking: how can both sides see each other's body in game. Initially we are planning to send the whole body data to other side, but the photon has trouble sending huge amount of data in short time. Thus, we trim down the data to a few key components: left/right hand transform/rotation, their respective 5 joints transform/rotation and head transform/rotation. We have a character model in the scene and we rendered his movement in Realtime given the data sent from other side. Also we make the data sending rate to be every 6 frames because of photon's bandwidth and we interpolate model's movement using transform and rotation.

The second challenge is how can we make the interactable items to be synchronous on both scenes.  We keep track of all interactable items in the room and push them to an update list so the other side can retrieve the Realtime position/rotation of the object. We also give host special control to some interactable items to ensure both scenes will stay synchronous. 

Future improvement: Initially we have setup IK (inverse kinematics) for the model to make it look natural. But because of Photon's limitation, the data it sends is not enough to make the model (especially arm joint) to look natural and smooth at all. If we have more time (2 week is a really short span for a multi-player VR game) we will try to add IK element in the game.
