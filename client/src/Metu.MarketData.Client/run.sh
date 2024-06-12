#!/usr/bin/env bash

function print_welcome() {
  PROGRAM_NAME="METU Market Data Interactive Bencmarking Framework"
  echo "$PROGRAM_NAME symbol_count connection_count symbols"
  echo ""
  echo " usage: ./run.sh 10 10 10 3"
  echo ""
  echo "params"
  echo " *SYM_COUNT max 8000, default 50"
  echo " *CONNECTION_COUNT max 100, default 1"
  echo " *LISTEN_DURATION default 60 seconds"
  echo " PROCESS_COUNT default 1"
  echo " INSTANCE_IP default per appsettings.json"
  echo " INSTANCE_PORT default per appsettings.json"
  echo " SYMBOL_FILE default syms.sym"
  echo ""
  echo " * required"
  echo " type _ (under_score) to use default"
  echo ""
}
print_welcome

SYM_COUNT=$1
if [[ -z "$SYM_COUNT" || $SYM_COUNT = "_" ]]; then
  SYM_COUNT=50
fi

CONNECTION_COUNT=$2
if [[ -z "$CONNECTION_COUNT" || $CONNECTION_COUNT = "_" ]]; then
  CONNECTION_COUNT=1
fi

LISTEN_DURATION=$3
if [[ -z "$LISTEN_DURATION" || $LISTEN_DURATION = "_" ]]; then
  LISTEN_DURATION=60
fi

PROCESS_COUNT=$4
if [[ -z "$PROCESS_COUNT" || $PROCESS_COUNT = "_" ]]; then
  PROCESS_COUNT=1
fi

INSTANCE_IP=$5
if [[ -z "$INSTANCE_IP" || $INSTANCE_IP = "_" ]]; then
  INSTANCE_IP=
fi

INSTANCE_PORT=$6
if [[ -z "$INSTANCE_PORT" || $INSTANCE_PORT = "_" ]]; then
  INSTANCE_PORT=
fi

SYMBOL_FILE=$7
if [[ -z "$SYMBOL_FILE" || $SYMBOL_FILE = "_" ]]; then
  SYMBOL_FILE=Resources/syms.sym
fi

if [[ ! -d ./publish ]]; then 
  dotnet publish --self-contained false -o ./publish
fi 

TODAY=$(date '+%Y-%m-%d')
mkdir logs || true
cd publish


for (( PROCESS_NUM=1 ; PROCESS_NUM<=$PROCESS_COUNT ; PROCESS_NUM++ ));
do
  
  LOG_FILE_NAME="../logs/${TODAY}-${SYM_COUNT}_${CONNECTION_COUNT}_${LISTEN_DURATION}_${PROCESS_NUM}.log"
  METRIC_FILE_NAME="../logs/${TODAY}-${SYM_COUNT}_${CONNECTION_COUNT}_${LISTEN_DURATION}_${PROCESS_NUM}.csv"
  BENCHMARK_FILE_NAME="../logs/${TODAY}-${SYM_COUNT}_${CONNECTION_COUNT}_${LISTEN_DURATION}_${PROCESS_NUM}.bench.csv"


  COMMAND_STRING=""
  COMMAND_STRING="${COMMAND_STRING} Serilog__WriteTo__Async__Args__configure__1__Args__configureLogger__WriteTo__0__Args__path=$LOG_FILE_NAME"
  COMMAND_STRING="${COMMAND_STRING} Serilog__WriteTo__Async__Args__configure__2__Args__configureLogger__WriteTo__0__Args__path=$METRIC_FILE_NAME"
  COMMAND_STRING="${COMMAND_STRING} Serilog__WriteTo__Async__Args__configure__3__Args__configureLogger__WriteTo__0__Args__path=$BENCHMARK_FILE_NAME"

  if [ ! -z "$INSTANCE_IP" ]; then
    COMMAND_STRING="${COMMAND_STRING} ConnectionSettings__Kdb__Host=${INSTANCE_IP}"
  fi

  if [ ! -z "$INSTANCE_PORT" ]; then
    COMMAND_STRING="${COMMAND_STRING} ConnectionSettings__Kdb__Port=${INSTANCE_PORT}"
  fi

  COMMAND_STRING="${COMMAND_STRING} ./Metu.MarketData.Client "
  COMMAND_STRING="${COMMAND_STRING} --SymbolFile $SYMBOL_FILE "
  COMMAND_STRING="${COMMAND_STRING} --SubCount $SYM_COUNT "
  COMMAND_STRING="${COMMAND_STRING} --ConnectionCount $CONNECTION_COUNT "
  COMMAND_STRING="${COMMAND_STRING} --ListenDuration $LISTEN_DURATION "
  COMMAND_STRING="${COMMAND_STRING} --InstanceNum ${PROCESS_NUM} "
  
  echo "launching instance $PROCESS_NUM"

  #eval "2>/dev/null 1>&2 ${COMMAND_STRING} &"
  eval "${COMMAND_STRING}"

done



