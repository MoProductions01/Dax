# Readme
- HOW TO PLAY-
Dax is a concentric rings puzzle game where you spin rings to guide the player
around the board, pick up items and avoid enemies.  To control a ring, just click on it
and drag.
If you want to just jump into a couple of sample leves (one for each mode) open up
    "Dax Collect Sample" or "Dax Color_Match".  How the modes work are described below
    in the "Puzzle Setup" section.
- ADDITIONAL INFO-
It's main unique feature is that it has an entire in-editor puzzle/level building system.
If you want to see a video demonstration of how it works, see here: <insert link>.
Below is a text explanation and more detailed description of what the various
Board Objects in the scene do. If you watch the video you can skip down to the 
    "BOARD OBJECTS/BUMPER SECTION" section below.

-------------------- YOU CAN SKIP TO "BOARD OBJECTS" SECTION IF YOU WATCH THE VIDEO ---------

Step 1: Creating the puzzle
    1) I did not include any scenes in the project to start from scratch, 
        but either way make sure you're in a blank scene.
    2) In the menu, click on Radient->Dax->Create Puzzle. The The blank puzzle is created
        in code (MCP.CreateNewPuzzle).
    3) In the Hierarchy, select the "DaxPuzzleSetup" object and lock it in the inspector.
        This is the window that the tool lives on.
    4) You can now press play and watch the puzzle in action, but there's not much going on yet.

Step 2: Puzzle Setup
    1) With the puzzle created and the Dax Puzzle Setup open, take a look at the inspector.
        The top part after the "Show Gizmos" checkbox is the section that controls the
        overall puzzle. Here's what they are
        -Name: This is the game of the puzzle that will get saved when it's saved to
            a binary format (see below).
        -Level Time: How long the level lasts before time runs out.
        - Victory Conditions.  Which mode the game is:
            - Collection: Must collect all facets on the ring.
            - Color_Match: Instead of collecting facets, you must pick them up and then
                take them to the appropriate bumper color.
        - Next is a list of all the different facet colors on the board.
        - Number of Rings: You choose between 1 and 4.
        - Init Puzzle: Must be clicked on after making changes to get the internal
            information set up.

Step 3: Puzzle Creation
    1) First thing to do is to start creating channels so the player has a place to move.
        To do this, select a gray channel piece and press B (you can also do this in the
        inspector with the "Turn Off/Turn On" button but because this is so common I 
        hooked it up to a button).  If you want to put the channel piece back, click on
        the red box and press B.  Give it a go and you can control the rings to move
        the player around the board.
    2) Once you have channels cleared, not that there are 3 circular nodes in each channel.
        The only node you can put objects on is the middle one, so select one of those and
        note the inspector. You will see a list of object categories you can add to the board.
        First Test:
            1) Add a few facets to the board.  Note that you can change the color in the inspector.
            2) Play in "Collection" mode and collect all facets.  That wins the game.
            3) Change to "Color_Match" mode.  Make sure you have at least 1 complete path to a bumper
                for each color facet you have.  Select the bumper, change bumper type to "Color_Match".
                Now when you play you carry the facets, so bring the facet to the correct color
                and it is collected.
            4) There's many other things you can add to the game, but I will be explaining what they all
                do in the "Board Objects" section.
    3) You can also modify the rings themselves.  Click on a dark gray wedge in between
        the channels to bring that up in the inspector.
        1) You can change the "Ring Speed" here.
        2) You can also Reset Ring, which gets rid of all objects and fills in the channels.

Step 4: Optional Binary Saves
    1) While of course saving the scene will work, I decided to add a system where you can save/load
        binary files with Serialization. 
    2) With the puzzle initialzed, go to the menu and click Radiend->Dax->Save Puzzle.  This will save
        the puzzle in binary format in the Resources/Puzzles folder (for some reason they don't show up
        until you open a new scene).
    3) Now Resources->Dax->Load Puzzle will bring up a menu where you can load up one of the binaries.


    ---------------------- BOARD OBJECTS/BUMPER SECTION.  EXPLAINS ALL THE THINGS YOU CAN PUT ON A NODE ---------------------
    NOTE: You can change the actual Board Object type without having to delete it first.  Just choose
        a new item from the drop down menu.
    Bumpers: These are the things at the end of each channel.  They have 3 settings:
        1) Regular - Player bounces off
        2) Death: Player dies if touches
        3) Color-Match: Used in the "Color_Match" game mode explained above.  In that  
            mode the player must drag the facet to the correct color bumper in order
            to collect it.  
    
    Player: User player.  You can adjust the speed with the slider.

    Functionality for all objects: 
        1) Ping Facet: This will highlight the object in the Hierarchy window.
        2) Delete <Object Type>: Deletes object from board (NOTE: Do not delete the
            objects from the hierarchy window )
        3) When you create an object, there's a pull down menu for the type of object.
            Changing the type after creation destroys the old object and creates a new one.

    OBJECT TYPES
        1) HAZARD
            ENEMY: creates a little demon that moves around the board like the player and
                can have it's path adjust by the player.
                - Start Direction: Starts moving inward or outward.
                - Speed: How fast it moves
            DYNAMITE: Kills player.  Nothing to adjust on this one.
            GLUE: Makes player stuck in place.
                - Effect Time: How long the player is stuck.
        2) FACET_COLLECT: NOTE: Must click on lightning bolt icon that appears.
            RING: Collects all facets on current ring.
            WHEEL: Clears whole board.
        3) SHIELD: NOTE: Must click on shield button icon that appears. Make sure to active them 
                before you collide with a Hazard.  Same time = death.
            HIT: Protects you from one collision with a Hazard. but does not
                destroy the Hazard.
            SINGLE_KILL: Kills 1 Hazard you collide with.
        4) SPEED: Increases the speed of various things. Each type has a "Speed Mod" slider to
                    adjust the actual speed mod.
            PLAYER_SPEED: Mods speed of Player.
            ENEMY_SPEED: Mods speed of enemy
            RING_SPEED: Mods speed of the ring the item is on.

        5) POINT_MOD: Modifies player score in various ways.
            EXTRA_POINTS: Awards extra points to the player
                - Point Mod Val: How many points to give to the player.
            POINTS_MULTIPLIER: How much each score will be multiplied
            TIMER: How long it lasts.





    

   
                
