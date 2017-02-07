This was a learning project as an introduction to machine learning. It uses a multi-layer feed forward network and supervised learning.

![Animation of learning in action](https://olenik.io/assets/images/bb8s.gif)

[Play with WebGL](https://olenik.io/demos/bb8learning/)

# Network Topology

The actors cast out 9 rays forward and to the side, acting as "feelers." These are fed into the network as inputs, along with velocity and a muliplier for raycast distance. Two hidden layers are used, and three outputs. They consist of speed, angle to turn, and multiplier for the feeler distance.

A debug screenshot showing feelers at different lengths:

![Debug screenshot of AI characters](https://olenik.io/assets/images/bb8learning-debugview.jpg)

When feeler length is reduced, the lateral distance between the tips of the lines is smaller, which allows for detecting and navigating through tighter spaces. When it's increased, it allows for detecting obstacles further away, allowing time to slow down. Combined with speed as one more input, they virtually never get stuck in tight spaces or get paralyzed when entering a spot where all obstacles are equidistant.

In other words, they move around naturally and don't trip up on difficult obstacles. At least for the environment given in this demo. I worked through many iterations before settling on this topology.

# Network Training

Learning is supervised, with a genetic algorithm with a fitness function of "journey length." This is the non-repeating, unique ground an actor covers during each generation. It's calculated by exploiting the handiness of Unity's physics engine -- a grid of trigger volumes are laid out onto the map, and moving forward through one (and not backwards) increments the journey length. A trigger can't be covered twice in the same generation. This prevents the selection of actors that just turn in circles.

# Why Little BB8s?

I was pleased with the complex behavior the network was able to handle. The movement is created with Unity's physics engine, so there are variables of static/dynamic friction and momentum to deal with. I did some compensation in code but I was pleased with that the topology of the network lent itself well to creating rather dynamic and believable behavior.
The first iteration of the project was just 2D, built with placeholders that were not easy on the eyes. A while earlier I had written a [BB8 character controller](/demos/bb8) for Unity, just as a fun project. I thought it'd be fun to make this project in 3D, and so that made a convenient prop. Although the hit detection is constrained to a 2D plane, the addition of angular momentum of a rolling character made the problem a lot more complicated.

The first iteration used just 1 hidden layer and had effective, but simplistic behavior. They moved in predictable patterns which were effective, but not very natural. They would reverse direction often, as if they were a highly indecisive squirrel. Using a rolling character introduced an additional challenge that actually ended up making the behavior much more natural.

