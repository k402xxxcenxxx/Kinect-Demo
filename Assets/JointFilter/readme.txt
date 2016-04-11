m_pFilter = new Sample::FilterDoubleExponential();
m_pFilter->Init(0.5,0.5,0.4,0.3,0.06);
m_pFilter->Update(joints, jointOrientations,0);
joints = m_pFilter->GetFilteredJoints();

this is how you use it
in the Update function the last parameter is the body id
so if you track more than one you have to pass the index there