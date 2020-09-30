APP?=dctrserver
PORT?=8001
APPPATH?=/mnt/f/GitHub/DCTR-system/src/DCTR_Server

RELEASE?=0.0.1
DOCKLOGIN?=ryutensai
CONTAINER_IMAGE?=docker.io/${DOCKLOGIN}/${APP}

GOOS?=linux
GOARCH?=386

clean:
	rm -f ${APP}

build: clean
	CGO_ENABLE=0 GOOS=${GOOS} GOARCH=${GOARCH} go build -a -installsuffix cgo -o ${APP}

container: build
	docker build -t $(CONTAINER_IMAGE):$(RELEASE) .

run: container
	docker stop $(APP):$(RELEASE) || true && docker rm $(APP):$(RELEASE) || true
	docker run \
			-v ${APPPATH}/templates:/templates \
			-v ${APPPATH}/credsStore:/credsStore \
			--name ${APP} -p ${PORT}:${PORT} --rm \
			-e "PORT=${PORT}" \
			$(DOCKLOGIN)/$(APP):$(RELEASE)

push: container
	docker push $(CONTAINER_IMAGE):$(RELEASE)

minikube: push
	for t in $(shell find ./kubernetes -type f -name "*.yaml"); do \
		cat $$t | \
			sed -E "s/\{\{(\s*)\.Release(\s*)\}\}/$(RELEASE)/g" | \
			sed -E "s/\{\{(\s*)\.ServiceName(\s*)\}\}/$(APP)/g"; \
		echo ---; \
	done > tmp.yaml
	kubectl apply -f tmp.yaml
