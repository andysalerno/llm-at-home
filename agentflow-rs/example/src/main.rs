use agentflow::{
    Agent, AgentCellConfig, AgentCellHandler, AgentName, CellHandler, ConsoleInputCellHandler,
    ConsoleUserAgent, ConversationState, Message, Role,
};
use cellflow::{
    Cell, CellHandlerConfig, CellVisitor, CustomCell, HandlerCollection, Id, SequenceCell,
    SequenceCellBuilder,
};
use log::info;
use serde::{Deserialize, Serialize};

fn main() {
    env_logger::init();

    // All handlers that will be available for the program:
    let handlers = HandlerCollection::new()
        .add(AgentCellHandler::new(DummyAgent, Id::new("DummyBot")))
        .add(AgentCellHandler::new(
            ConsoleUserAgent::new(),
            Id::new("ConsoleUser"),
        ))
        .add(ReplyWithMessageCellHandler)
        .add(ConsoleInputCellHandler::new());

    // The program definition, which will be run:
    let program = SequenceCellBuilder::new()
        .add(CustomCell::new(AgentCellConfig::new(
            "unset".into(),
            Id::new("DummyBot"),
        )))
        .add(CustomCell::new(AgentCellConfig::new(
            "unset".into(),
            Id::new("ConsoleUser"),
        )))
        .build();

    let program: Cell = program.into();

    // Debug
    {
        let json = serde_json::to_string(&program).unwrap();
        info!("Program: {json}");
    }

    let visitor = CellVisitor::new(handlers);

    let output = visitor.run(&program, &ConversationState::empty());

    info!("Output: {:?}", output);
}

#[derive(Debug)]
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
    fn cell_type_id() -> Id {
        Id::new("reply-with-message")
    }
}

#[derive(Debug)]
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
