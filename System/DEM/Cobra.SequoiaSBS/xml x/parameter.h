#ifndef __PARAMETER_H__
#define __PARAMETER_H__

#define	PARM_TYPE_MASK			0xF00
#define	PARM_INDEX_MASK			0x0FF
#define	PARM_BCFG_BASE			0x100
#define	PARM_SWP_BASE			0x200
#define	PARM_HWP_BASE			0x300
#define	PARM_MFG_BASE			0x400
#define	PARM_MFG_STR_MAX		32

#define	PARM_UPDATE_RD			0xBB
#define	PARM_UPDATE_WR			0xAA

typedef	enum {
	PARM_BCFG_MAX,
}	PARAM_BOARD_CFG;

typedef	enum {
	PARM_SWP_MAX
}	PARAM_SW_PROT;

typedef	enum {
	PARM_HWP_MAX
}	PARAM_HW_PROT;

typedef	enum {	
	PARM_MFG_MAX
}	PARAM_MFG_STR;


extern volatile signed int param_board_cfg[(PARM_BCFG_MAX)];
extern volatile signed int param_sw_prot[(PARM_SWP_MAX)];
extern volatile signed int param_hw_prot[(PARM_HWP_MAX)];
extern volatile char param_mfg_str[PARM_MFG_MAX][PARM_MFG_STR_MAX];


#endif	//__PARAMETER_H__
