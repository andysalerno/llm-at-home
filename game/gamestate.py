from dataclasses import dataclass, field


@dataclass
class Node:
    parent: "Node | None" = None

    depth: int = 0
    """
    The depth of the node in the tree.
    Will impact the likelihood of 'game over'
    """

    is_game_over: bool = False

    internal_text: str = ""
    """
    Contextual information decided by the LLM that is NOT shown to the player.
    Why? Because there might be "hidden state" like a box that has not yet been opened.
    We must know immediately what is inside the box, before the player does,
    so that any child states that involve opening the box will see the same item (as an example).
    """

    story_text: str = ""

    children: list["Node"] = field(default_factory=list)

    def is_root(self) -> bool:
        return self.parent is None


def generate_tree() -> Node:
    pass
