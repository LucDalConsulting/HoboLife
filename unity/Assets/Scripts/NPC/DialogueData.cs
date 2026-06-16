using System.Collections.Generic;

// HoboLife — dialogue trees ported from the web prototype. A node has text and up
// to four options. An option either jumps to another node, leaves, starts a fight,
// or runs a dice-gated skill check with success/fail outcomes. DCs match the web
// build: panhandle 30, charm 50, calm-the-thug 60.
public static class DialogueData
{
    public class Outcome
    {
        public string text;       // line shown on resolution
        public int money;         // money delta
        public string addSkill;   // "int"/"cha"/"str"/"tool" or null
        public int addAmount;
        public string then;       // "end" | "fight" | nodeId
    }

    public class Option
    {
        public string label;
        public string then;       // for non-check options: "end" | "fight" | nodeId
        public string checkSkill; // null = no check; otherwise "cha" etc.
        public int dc;
        public Outcome success, fail;
    }

    public class Node
    {
        public string text;
        public List<Option> options;
    }

    public static Dictionary<string, Node> Tree(string id) => id == "thug" ? Thug : Pedestrian;

    static Option Opt(string label, string then) => new Option { label = label, then = then };
    static Option Check(string label, string skill, int dc, Outcome s, Outcome f)
        => new Option { label = label, checkSkill = skill, dc = dc, success = s, fail = f };
    static Outcome Out(string text, string then) => new Outcome { text = text, then = then };

    public static readonly Dictionary<string, Node> Pedestrian = new Dictionary<string, Node>
    {
        { "start", new Node { text = "What do you want?", options = new List<Option>
            {
                Check("Spare some change?", "cha", 30,
                    new Outcome { text = "Alright, here's a few bucks.", money = 12, then = "end" },
                    Out("Get a job, bum.", "end")),
                Check("You look great today.", "cha", 50,
                    new Outcome { text = "Aw... thanks, stranger!", addSkill = "cha", addAmount = 1, then = "end" },
                    Out("...weirdo.", "end")),
                Opt("Never mind.", "end"),
            } } },
    };

    public static readonly Dictionary<string, Node> Thug = new Dictionary<string, Node>
    {
        { "start", new Node { text = "You lookin' at me, bum?", options = new List<Option>
            {
                Opt("Sorry — my mistake.", "end"),
                Opt("Bring it.", "fight"),
                Check("Easy now, no trouble.", "cha", 60,
                    Out("Tch. Get outta here.", "end"),
                    new Outcome { text = "Wrong answer.", then = "fight" }),
            } } },
    };
}
