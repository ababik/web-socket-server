const url = "ws://localhost:8080/ws";

function connect() {
    let socket = new WebSocket(url);
    socket.onopen = () => {
        socket.send(JSON.stringify({ type: "test", value: "test" }));
    }
    socket.onmessage = (event) => {
        console.log("onmessage:", event);
    }
    socket.onerror = (event) => {
        console.log("onerror:", event);
        socket.close();
    }
    socket.onclose = (event) => {
        console.log("onclose:", event);
    }
}

connect();