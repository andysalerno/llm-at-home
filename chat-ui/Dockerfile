FROM node:20 as base-node-image

RUN git clone https://github.com/huggingface/chat-ui
WORKDIR /chat-ui
RUN git checkout fc5262974a556bf308943770a961b7ccbf8dcb4e
RUN rm -rf .git

RUN rm package.json && rm .env
COPY ./.env .
COPY ./package.json .

RUN npm install

CMD ["npm", "run", "prod"]