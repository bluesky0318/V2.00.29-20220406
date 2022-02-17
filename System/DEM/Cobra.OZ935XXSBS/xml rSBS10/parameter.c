

#include "parameter.h"


//Board Configuration
volatile signed int param_board_cfg[(PARM_BCFG_MAX)] = {
};

//Software Protection
volatile signed int param_sw_prot[(PARM_SWP_MAX)] = {
};

//Hardware Protection
volatile signed int param_hw_prot[(PARM_HWP_MAX)] = {
};

//String information
volatile char param_mfg_str[PARM_MFG_MAX][PARM_MFG_STR_MAX] = {

};


