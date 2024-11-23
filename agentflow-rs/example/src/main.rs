use agentflow::{
    Agent, AgentCellConfig, AgentCellHandler, AgentName, CellHandler, ConversationState, Message,
    Role,
};
use cellflow::{Cell, CellVisitor, CustomCell, Id, SequenceCell};
use log::info;
use serde::{Deserialize, Serialize};

fn main() {
    env_logger::init();

    let program: Cell = SequenceCell::new(vec![CustomCell::new(
        AgentCellHandler::name(),
        AgentCellConfig::new("WebAgent".into()),
    )
    .into()])
    .into();

    let handlers = vec![
        AgentCellHandler::new(DummyAgent).into_handler(),
        ReplyWithMessageCellHandler.into_handler(),
    ];

    let visitor = CellVisitor::new(handlers);

    let output = visitor.run(&program, &ConversationState::empty());

    info!("Output: {:?}", output);

    let json = serde_json::to_string(&program).unwrap();
    info!("Program: {:?}", json);
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

        let cell = CustomCell::new(Id::new("reply-with-message-cell"), cell);

        SequenceCell::new(vec![cell.into()]).into()
    }
}

#[derive(Serialize, Deserialize, Clone, Debug)]
struct ReplyWithMessageCellConfig {
    message: Message,
}

impl ReplyWithMessageCellConfig {
    fn new(message: Message) -> Self {
        Self { message }
    }
}

struct ReplyWithMessageCellHandler;

impl CellHandler<ConversationState> for ReplyWithMessageCellHandler {
    type Config = ReplyWithMessageCellConfig;

    fn name(&self) -> Id {
        Id::new("reply-with-message-cell")
    }

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
