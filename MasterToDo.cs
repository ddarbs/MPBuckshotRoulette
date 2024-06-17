using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterToDo : MonoBehaviour
{
    //things to do
    // lobby system multiple games going at once
    // turn based system
    // ONCE SHOTGUN IS LOADED
    // both players roll dice to see who goes first each player rolls then highest wins
    // (If items enabled both players get offered items here)
    // first player can choose to use items or shotgun
    // (if Item is selected it willbe used here before shotgun, can use as many items as they want?)
    // first player chooses shotgun then they choose who they point it at
    // (if damage is done both players see updated health)
    // repeat for player 2
    // Winner gets a victory screen, then both players can agree on a rematch?
    // Camera angles
    //1- Zoom in on the shotgun shells
    //2- Zoom in on loading
    //3 - Default view of players facing eachother
    //4 - looking down at items
    //5 - looking at health / score screen
    //6 - some sort of getting revived seen between shotgun shots
    

/* darb notes
 *      - turn based system mostly done, commented lot of areas in TurnSystem script where camera stuff could go.
 *      - did a test on connecting the lobby to the game scene, and have a way to handle grabbing the config options.
 *      - done: add AudioSystem functions in for the other sounds and start implementing where possible (hp, loading, shooting, etc).
 *      - done: implement items w/placeholder models
 *      - done: figure out the other cfg options we want to have in the lobby, don't need to finalize until lobby is connected
 *      - done: start working on the scene visuals while Michael does camera stuff
 *      - done: make a placeholder TurnSystem function to reset the game when someone dies
 *      - done: hover description text UI for items
 *      - done: audio mixer and volume slider/mute button
 *      - done: finish connecting lobby to game scene
 *      - todo: handle quitting/dcing from game scene 
 *      - todo: button that returns to lobby scene from game scene 
 *      - done: lobby kick player2 button
 *      - todo: visuals - (done)blood effect when shot, item use effects (think just magnifying glass and phone still text stuff), maybe change table model
 *      - done: test observer conditions w/multiple lobbies going at the same time 
 *      - done: disable physics raycaster during camera movements so they cant hover/click on items
 *      - done: pause turn when using an item
 *      - done: temp turning for shotgun, racking shotgun
 *      - done: spawn shells on round start
 *      - done: eject shells (just shoot out the side and let networktransform do it's thing, will need a coroutine to control movement since don't want rigidbody) 
 *      - done: pause for some sort of victory screen/effect (maybe lights show above the winner and a jingle plays?), (already done)then go back to ready up screen 
 *      - todo: more sound stuff - (done)audio on shell spawn, audio on item spawns, (done for now)have item use wait until item audio finished playing to do stuff
 *      - todo: change ready up/self/other button ui
 *      - todo: tweak the wait timings
 *      - done: version check on connect screen
 *      - todo: (done)dim music when using item, (done)game over, ?
 *      - done: cap items used per turn, show item use ui (setup so we could make this a lobby option if wanted)
 *      - done: swap shotgun pump and shell eject from server to local
 *      - done: fix bug where dying from pills during your turn doesn't end the game
 *      - todo: scale testing up to multiple lobbies
 *      - todo: scale the lobby browser scrollview height by number of lobbies
 *      
 *
 */


/*
Ideas:
    - item that causes your opponent's next used item to have a 50% fail chance?
    
*/
}
