use agentflow::{
    Agent, AgentCellConfig, AgentCellHandler, AgentName, CellHandler, ConversationState, Message,
    Role,
};
use cellflow::{
    Cell, CellHandlerConfig, CellVisitor, CustomCell, HandlerCollection, Id, SequenceCell,
    SequenceCellBuilder,
};
use log::info;
use serde::{Deserialize, Serialize};

fn main() {
    env_logger::init();

    let program = SequenceCellBuilder::new()
        .add(CustomCell::new(AgentCellConfig::new("WebAgent".into())))
        .add(CustomCell::new(AgentCellConfig::new("WebAgent".into())))
        .build();

    let program: Cell = program.into();

    let handlers = HandlerCollection::new()
        .add(AgentCellHandler::new(DummyAgent))
        .add(ReplyWithMessageCellHandler);

    let visitor = CellVisitor::new(handlers);

    let output = visitor.run(&program, &ConversationState::empty());

    info!("Output: {:?}", output);

    let json = serde_json::to_string(&program).unwrap();
    info!("Program: {json}");
}

struct DummyAgent;

impl Agent for DummyAgent {
    fn name(&self) -> AgentName {
        AgentName::new("DummyAgent")
    }

    fn role(&self) -> Role {
        Role::new("DummyAgent")
    }

    fn behavior(&self) -> cellflow::Cell {
        let cell = ReplyWithMessageCellConfig::new(Message::new(
            self.name(),
            self.role(),
            "Hi!".to_string(),
        ));

        let cell = CustomCell::new(cell);

        SequenceCell::new(vec![cell.into()]).into()
    }
}

#[derive(Serialize, Deserialize, Clone, Debug)]
struct ReplyWithMessageCellConfig {
    message: Message,
}

impl ReplyWithMessageCellConfig {
    const fn new(message: Message) -> Self {
        Self { message }
    }
}

impl CellHandlerConfig for ReplyWithMessageCellConfig {
    fn id() -> Id {
        Id::new("reply-with-message")
    }
}

struct ReplyWithMessageCellHandler;

impl CellHandler<ConversationState> for ReplyWithMessageCellHandler {
    type Config = ReplyWithMessageCellConfig;

    fn evaluate(
        &self,
        item: &ConversationState,
        cell_config: &Self::Config,
        _visitor: &CellVisitor<ConversationState>,
    ) -> ConversationState {
        let mut item = item.clone();
        item.add_message(cell_config.message.clone());
        item
    }
}
