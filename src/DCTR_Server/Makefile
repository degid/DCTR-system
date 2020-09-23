APP?=dctrserver
PORT?=8001

clean:
	rm -f ${APP}

build: clean
	go build -o ${APP}

run: build
	PORT=${PORT} ./${APP}
	