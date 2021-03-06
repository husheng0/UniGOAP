﻿using System.Collections.Generic;

using StateFlag = System.Collections.Generic.KeyValuePair<string, object>;
using Plan = System.Collections.Generic.Queue<UniGOAP.Action>;

namespace UniGOAP {
    public class Planner {
        private class Node {
            public Node parent;
            public float cost;
            public HashSet<StateFlag> state;
            public Action action;

            public Node(Node pParent, float pCost, HashSet<StateFlag> pState, Action pAction) {
                parent = pParent;
                cost = pCost;
                state = pState;
                action = pAction;
            }
        }

        public Plan CreatePlan(Agent pAgent, HashSet<Action> pAvailableActions, HashSet<StateFlag> pWorldState, HashSet<StateFlag> pGoalState) {
            Logger.Log("________________CREATING NEW PLAN________________");
            Logger.Log("Available actions : " + pAvailableActions.PrettyPrint());
            Logger.Log("World state : " + pWorldState.PrettyPrint());
            Logger.Log("Goal state : " + pGoalState.PrettyPrint());

            foreach (Action a in pAvailableActions)
                a.Reset();

            HashSet<Action> lUsableActions = new HashSet<Action>();
            foreach (Action a in pAvailableActions)
                if (a.CheckProceduralPrecondition(pAgent))
                    lUsableActions.Add(a);
            Logger.Log("Usable actions : " + lUsableActions.PrettyPrint());

            List<Node> lLeaves = new List<Node>();
            Node lStart = new Node(null, 0, pWorldState, null);
            bool lSuccess = BuildGraph(lStart, lLeaves, lUsableActions, pGoalState);
            
            if (!lSuccess) 
                return null;
            Node lCheapsetLeaf = null;
            foreach(Node _leaf in lLeaves) {
                if (lCheapsetLeaf == null)
                    lCheapsetLeaf = _leaf;
                else if (_leaf.cost < lCheapsetLeaf.cost)
                    lCheapsetLeaf = _leaf;
            }

            List<Action> _result = new List<Action>();
            Node n = lCheapsetLeaf;
            while(n != null) {
                if (n.action != null)
                    _result.Insert(0, n.action);
                n = n.parent;
            }

            Plan lQueue = new Plan();
            foreach (Action a in _result)
                lQueue.Enqueue(a);

            return lQueue;
        }

        bool BuildGraph(Node pStartNode, List<Node> pLeaves, HashSet<Action> pUsableActions, HashSet<StateFlag> pGoalState) {
            bool lFoundAnActionPath = false;

            foreach(Action _action in pUsableActions) {
                // Check if the current state satisfies all the preconditions of the action we are considering 
                if (!Utils.InState(_action.GetPreconditions(), pStartNode.state)) 
                    continue;

                // If yes, we merge onto the startnode all the effects of that action
                var _newState = Utils.EnsureSubset(pStartNode.state, _action.GetEffects());
                Node _node = new Node(pStartNode, pStartNode.cost + _action.cost, _newState, _action);

                if(Utils.InState(pGoalState, _newState)) {
                    pLeaves.Add(_node);
                    lFoundAnActionPath = true;
                }
                else {
                    HashSet<Action> _subset = Utils.RemoveFromActions(pUsableActions, _action);
                    if (BuildGraph(_node, pLeaves, _subset, pGoalState))
                        lFoundAnActionPath = true;
                }
            }
            return lFoundAnActionPath;
        }
    }
}
